using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

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

    public static class TransformationFactory
    {
        public static ITransformation GetTransformation(string transformation, params string[] args)
        {
            switch (transformation)
            {
                case "Empty":
                    var emptyTrans = new EmptyTransformation();
                    return emptyTrans;

                case "DropColumn":
                    var dropTrans = new DropColumnTransformation();
                    foreach (var arg in args)
                        dropTrans.DropHeaders.Add(new Header(arg));
                    return dropTrans;

                case "SortBy":
                    var sortTrans = new SortByTransformation();
                    sortTrans.SortBy = new Header(args[0]);
                    if (args[1] == "Asc")
                        sortTrans.Direction = SortDirection.Ascending;
                    else if (args[1] == "Desc")
                        sortTrans.Direction = SortDirection.Descending;
                    else
                        throw new ArgumentException($"SortDirection \"{args[1]}\" unrecognized");
                    return sortTrans;

                case "GroupBy":
                    var groupTrans = new GroupByTransformation();
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for GroupBy transformation");
                    groupTrans.TargetHeader = new Header(args[0]);
                    if (args[1] == "Sum")
                        groupTrans.GroupAgregation = Agregation.Sum;
                    else if (args[1] == "Avg")
                        groupTrans.GroupAgregation = Agregation.Mean;
                    else if (args[1] == "Concat")
                        groupTrans.GroupAgregation = Agregation.ConcatValues;
                    else if (args[1] == "CountDistinct")
                        groupTrans.GroupAgregation = Agregation.CountDistinct;
                    else if (args[1] == "CountAll")
                        groupTrans.GroupAgregation = Agregation.CountAll;
                    else if (args[1] == "GroupKey")
                        groupTrans.GroupAgregation = Agregation.GroupKey;
                    else
                        throw new ArgumentException($"Agregation \"{args[1]}\" not supported");
                    for(int i = 2; i < args.Length; i++)
                        groupTrans.StringsToGroup.Add(args[i]);
                    return groupTrans;

                case "FilterBy":
                    var filterTrans = new FilterByTransformation();
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for FilterBy transformation");
                    if (args[0] == "==")
                    {
                        filterTrans.FilterCondition.Relation = Relation.Equals;
                        filterTrans.FilterCondition.Source = new Header(args[1]);
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else if (args[0] == "!=")
                    {
                        filterTrans.FilterCondition.Relation = Relation.NotEquals;
                        filterTrans.FilterCondition.Source = new Header(args[1]);
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else if (args[0] == "<")
                    {
                        filterTrans.FilterCondition.Relation = Relation.LessThan;
                        filterTrans.FilterCondition.Source = new Header(args[1]);
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else if (args[0] == ">")
                    {
                        filterTrans.FilterCondition.Relation = Relation.GreaterThan;
                        filterTrans.FilterCondition.Source = new Header(args[1]);
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else
                    {
                        throw new ArgumentException($"Operation \"{args[0]}\" not supported");
                    }
                    return filterTrans;
                default:
                    throw new ArgumentException($"Transformation \"{transformation}\" not supported");
            }
        }
    }

    public interface ITransformation
    {
        public TransformationType Type { get;}

        public List<Field> PerformTransformation(List<Field> input_list);

        public List<EmptyField> Preprocess(List<EmptyField> list);

        public string GetOperator();
    }

    public class EmptyTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.Empty;

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            return input_fields;
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }

        public string GetOperator() => "Empty";
    }

    public class DropColumnTransformation : ITransformation
    { 
        public TransformationType Type => TransformationType.DropColumns;

        public HashSet<Header> DropHeaders { get; set; }

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            var selected_fields =
                 from field in input_fields
                 where !DropHeaders.Contains(field.Header)
                 select field;
            return selected_fields.ToList();
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            var selected_fields =
                 from field in list
                 where !DropHeaders.Contains(field.Header)
                 select field;
            return selected_fields.ToList();
        }

        public string GetOperator() => "DropColumn";
    }

    public class SortByTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.SortBy;

        public SortDirection Direction { get; set; }

        public Header SortBy { get; set; }

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            Field? source_field = input_fields.FirstOrDefault(x => x.Header == SortBy);
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

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }

        public string GetOperator() => "SortBy";
    }

    public class GroupByTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.GroupBy;

        public HashSet<string> StringsToGroup { get; set; }

        public Agregation GroupAgregation { get; set; }

        public Header TargetHeader { get; set; }

        public List<Field> PerformTransformation(List<Field> fields)
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

        public List<EmptyField> Preprocess(List<EmptyField> list)
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

        public string GetOperator() => "GroupBy";
    }

    public class FilterByTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.FilterBy;

        public FilterCondition FilterCondition { get; set; }

        public List<Field> PerformTransformation(List<Field> fields)
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

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list;
        }

        public string GetOperator() => "FilterBy";
    }

    public static class Transformator
    {
        public static List<Field> TransformFields(List<Field> fields, List<ITransformation> transformations)
        {
            foreach (var transformation in transformations)
            {
                fields = transformation.PerformTransformation(fields);
            }
            return fields;
        }
    }
}