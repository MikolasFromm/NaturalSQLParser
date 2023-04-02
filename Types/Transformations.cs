using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;

// user_custom definitions
using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;

namespace NaturalSQLParser.Types.Tranformations
{
    public static class ListExtensions
    {
        public static List<int> GetIndexes(this List<Cell> cells)
        {
            var indexes =
                from cell in cells
                select cell.Index;
            return indexes.ToList();
        }

        public static List<int> SortAndGetIndexes(this Field field)
        {
            field.Data.Sort();
            return field.Data.GetIndexes();
        }

        public static List<Field> ReArrangeAndSelectByIndex(this List<Field> fieldList, Header fieldToIgnore, List<int> indexes)
        {
            for (int i = 0; i < fieldList.Count; i++)
            {
                if (fieldList[i].Header != fieldToIgnore)
                {
                    Field newField = new() { Header = fieldList[i].Header };
                    foreach (var index in indexes)
                    {
                        newField.Data.Add(fieldList[i].Data[index]);
                    }
                    fieldList[i] = newField;
                }
            }
            return fieldList;
        }
    }

    public abstract class BasicTransformation
    {
        public abstract TransformationType Type { get;}

        public abstract List<Field> PerformTransformation(List<Field> input_list);

        public abstract List<EmptyField> Preprocess(List<EmptyField> list);
    }

    public class EmptyTransformation : BasicTransformation
    {

        public override TransformationType Type { get; } = TransformationType.Empty;

        public override List<Field> PerformTransformation(List<Field> input_fields)
        {
            return input_fields;
        }

        public override List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }
    }

    public class DropColumnTransformation : BasicTransformation
    {
        public HashSet<Header> DropHeaders { get; set; }

        public override TransformationType Type { get; } = TransformationType.DropColumns;

        public override List<Field> PerformTransformation(List<Field> input_fields)
        {
            var selected_fields =
                 from field in input_fields
                 where !DropHeaders.Contains(field.Header)
                 select field;
            return selected_fields.ToList();
        }

        public override List<EmptyField> Preprocess(List<EmptyField> list)
        {
            var selected_fields =
                 from field in list
                 where !DropHeaders.Contains(field.Header)
                 select field;
            return selected_fields.ToList();
        }
    }

    public class SortByTransformation : BasicTransformation
    {
        public SortDirection Direction { get; set; }

        public Header SortSource { get; set; }

        public override TransformationType Type { get; } = TransformationType.SortBy;

        public override List<Field> PerformTransformation(List<Field> input_fields)
        {
            Field? source_field = input_fields.FirstOrDefault(x => x.Header == SortSource);
            if (source_field is not null)
            {
                var indexes = source_field.SortAndGetIndexes();
                return input_fields.ReArrangeAndSelectByIndex(source_field.Header, indexes);
            }
            else
            {
                throw new ArgumentException("Field to SortBy not found.");
            }
        }

        public override List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }
    }

    public class GroupByTransformation : BasicTransformation
    {
        public HashSet<string> StringsToGroup { get; set; }

        public Agregation GroupAgregation { get; set; }

        public Header TargetHeader { get; set; }

        public override TransformationType Type { get; } = TransformationType.GroupBy;

        public override List<Field> PerformTransformation(List<Field> fields)
        {
            Field? field = fields.FirstOrDefault(x => x.Header == TargetHeader);
            if (field is not null)
            {
                // create groups
                var grouped_cells =
                    from cell in field.Data
                    where StringsToGroup.Contains(cell.Content)
                    group cell by cell.Content into newGroup
                    orderby newGroup.Key
                    select newGroup.ToList();

                // create one list from the groups
                var grouped = new List<Cell>();
                foreach (var group in grouped_cells)
                    grouped.Concat(group);
                field.Data = grouped;

                // ReArrange the fields that are left
                var indexes = field.Data.GetIndexes();
                return fields.ReArrangeAndSelectByIndex(field.Header, indexes);
            }
            else
            {
                throw new ArgumentException("Field to GroupBy not found.");
            }
        }

        public override List<EmptyField> Preprocess(List<EmptyField> list)
        {
            switch (GroupAgregation)
            {
                case Agregation.CountAll:
                case Agregation.CountDistinct:
                case Agregation.Sum:
                case Agregation.Mean:
                    foreach (var field in list)
                        field.Header.Type = FieldDataType.Number;
                    return list;
                case Agregation.GroupKey:
                case Agregation.ConcatValues:
                    return list;

                default:
                    throw new ArgumentException("Unknown Agregation");
            }
        }
    }

    public class FilterByTransformation : BasicTransformation
    {
        public FilterLogicalOperator FilterOperator { get; set; }

        public FilterCondition FilterCondition { get; set; }

        public override TransformationType Type { get; } = TransformationType.FilterBy;

        public override List<Field> PerformTransformation(List<Field> fields)
        {
            Field? field = fields.FirstOrDefault(x => x.Header == FilterCondition.Source);
            if (field is not null)
            {
                switch (FilterCondition.Relation)
                {
                    case Relation.Equals:

                        var eq_result = from cell in field.Data where cell.Content == FilterCondition.Condition select cell;
                        field.Data = eq_result.ToList();
                        var eq_indexes = field.Data.GetIndexes();
                        fields.ReArrangeAndSelectByIndex(field.Header, eq_indexes);
                        return fields;

                    case Relation.NotEquals:

                        var neq_result = from cell in field.Data where cell.Content != FilterCondition.Condition select cell;
                        field.Data = neq_result.ToList();
                        var neq_indexes = field.Data.GetIndexes();
                        fields.ReArrangeAndSelectByIndex(field.Header, neq_indexes);
                        return fields;

                    case Relation.LessThan:

                        var lt_result = from cell in field.Data where String.Compare(cell.Content, FilterCondition.Condition) < 0 select cell;
                        field.Data = lt_result.ToList();
                        var lt_indexes = field.Data.GetIndexes();
                        fields.ReArrangeAndSelectByIndex(field.Header, lt_indexes);
                        return fields;

                    case Relation.GreaterThan:

                        var gt_result = from cell in field.Data where String.Compare(cell.Content, FilterCondition.Condition) > 0 select cell;
                        field.Data = gt_result.ToList();
                        var gt_indexes = field.Data.GetIndexes();
                        fields.ReArrangeAndSelectByIndex(field.Header, gt_indexes);
                        return fields;

                    case Relation.InRange:
                        throw new NotImplementedException("Relation.InRange not implemented");

                    default:
                        throw new ArgumentException("Unknown Relation operator given.");
                }
            }
            else
            {
                throw new ArgumentException("Field to FilterBy not found.");
            }
        }

        public override List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }
    }

    public static class Transformator
    {
        public static List<Field> TransformFields(List<Field> fields, List<BasicTransformation> transformations)
        {
            foreach (var transformation in transformations)
            {
                fields = transformation.PerformTransformation(fields);
            }
            return fields;
        }
    }
}