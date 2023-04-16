// user_custom definitions
using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;

namespace NaturalSQLParser.Types.Tranformations
{
    public static class ListExtensions
    {
        /// <summary>
        /// Returns indices of given cells from the collection
        /// </summary>
        /// <param name="cells">List of cells from which to obtain the indices</param>
        /// <returns><see cref="IEnumerable{int}"/> collection of indices</returns>
        public static IEnumerable<int> GetIndexes(this IEnumerable<Cell> cells)
        {
            var indexes =
                from cell in cells
                select cell.Index;
            return indexes;
        }

        /// <summary>
        /// Sorts the data in the field and returns the indices of the sorted data
        /// </summary>
        /// <param name="field">Field to sort</param>
        /// <returns><see cref="IEnumerable{int}"/> collection of indices of the cells from the field</returns>
        public static IEnumerable<int> SortAndGetIndexes(this Field field)
        {
            field.Data.Sort();
            return field.Data.GetIndexes();
        }

        /// <summary>
        /// Recreates a list of fields where specific header might be dropped and only those cells from the field which coresponds to the given indices are kept.
        /// </summary>
        /// <param name="fieldList"></param>
        /// <param name="fieldToIgnore"></param>
        /// <param name="indexes"></param>
        /// <returns><see cref="List{Field}"/> collection of rearranged Fields.</returns>
        public static List<Field> ReArrangeAndSelectByIndex(this List<Field> fieldList, Header fieldToIgnore, IEnumerable<int> indexes)
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
        /// <summary>
        /// Only returns empty transformation class for obtaining the corrent moves and arguments.
        /// </summary>
        /// <param name="transformation">String representation of the transformation</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When unknown transformation given.</exception>
        public static ITransformation GetTransformationCandidate(string transformation)
        {
            switch (transformation)
            {
                case "Empty":
                    return new EmptyTransformation();
                case "DropColumn":
                    return new DropColumnTransformation();
                case "SortBy":
                    return new SortByTransformation();
                case "GroupBy":
                    return new GroupByTransformation();
                case "FilterBy":
                    return new FilterByTransformation();
                default:
                    throw new ArgumentException("Unknown transformation");
            }
        }

        /// <summary>
        /// Transformation builder, which already expects all input arguments in given order and in corrent format.
        /// It is expected that user first calls <see cref="GetTransformationCandidate(string)"/> to obtain all moves and arguments.
        /// </summary>
        /// <param name="transformation"><see cref="String"/> representation of a transformation.</param>
        /// <param name="args">All arguments given for the transformation.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When wrong arguments given.</exception>
        public static ITransformation BuildTransformation(string transformation, params string[] args)
        {
            switch (transformation)
            {
                case "Empty":
                    var emptyTrans = new EmptyTransformation();
                    return emptyTrans;

                case "DropColumn":
                    if (args.Length < 1)
                        throw new ArgumentException("Not enough arguments for DropColumn transformation");

                    var dropTrans = new DropColumnTransformation();
                    foreach (var arg in args)
                        dropTrans.DropHeaderNames.Add(arg);
                    return dropTrans;

                case "SortBy":
                    if (args.Length < 2)
                        throw new ArgumentException("Not enough arguments for SortBy transformation");

                    var sortTrans = new SortByTransformation();
                    sortTrans.SortByHeaderName = args[0]; // gets the header of the field by which to sort
                    if (args[1] == "Asc") // if ascending or descending
                        sortTrans.Direction = SortDirection.Ascending;
                    else if (args[1] == "Desc") // if ascending or descending
                        sortTrans.Direction = SortDirection.Descending;
                    else
                        throw new ArgumentException($"SortDirection \"{args[1]}\" unrecognized");
                    return sortTrans;

                case "GroupBy":
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for GroupBy transformation");
                    
                    var groupTrans = new GroupByTransformation();

                    groupTrans.TargetHeaderName = args[0]; // gets the header of the field by which to group
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
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for FilterBy transformation");
                    
                    var filterTrans = new FilterByTransformation();
                    filterTrans.FilterCondition.SourceHeaderName = args[1];

                    if (args[1] == "==")
                    {
                        filterTrans.FilterCondition.Relation = Relation.Equals;
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else if (args[1] == "!=")
                    {
                        filterTrans.FilterCondition.Relation = Relation.NotEquals;
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else if (args[1] == "<")
                    {
                        filterTrans.FilterCondition.Relation = Relation.LessThan;
                        filterTrans.FilterCondition.Condition = args[2];
                    }
                    else if (args[1] == ">")
                    {
                        filterTrans.FilterCondition.Relation = Relation.GreaterThan;
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

        /// <summary>
        /// Makes the real final transformation on the given field.
        /// </summary>
        /// <param name="input_list"></param>
        /// <returns></returns>
        public List<Field> PerformTransformation(List<Field> input_list);

        /// <summary>
        /// Makes the transformation only with <see cref="EmptyField"/>, which is used to get all future transformations.
        /// </summary>
        /// <param name="list">Current dataset</param>
        /// <returns></returns>
        public List<EmptyField> Preprocess(List<EmptyField> list);

        /// <summary>
        /// Returns string represenation of the function operator assigned to the transformation.
        /// Should be given to the OpenAI api to get all possibilities.
        /// </summary>
        /// <returns><see cref="String"/> representation of the transformation.</returns>
        public string GetTransformationName();

        /// <summary>
        /// Returns human instructions for the next moves.
        /// </summary>
        /// <returns></returns>
        public string GetNextMovesInstructions();

        /// <summary>
        /// Returns all possible next moves when the transformation is invoked.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields);

        /// <summary>
        /// Returns human instructions for the arguments that should be given.
        /// </summary>
        /// <returns></returns>
        public string GetArgumentsInstructions();

        /// <summary>
        /// Returns all possible arguments related to the possible moves.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetArguments();
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

        public string GetTransformationName() => "Empty";

        public string GetNextMovesInstructions() => "Empty transformation has no next moves";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields) => new List<string>();

        public string GetArgumentsInstructions() => "Empty transformation has no arguments";

        public IEnumerable<string> GetArguments() => new List<string>();

    }

    public class DropColumnTransformation : ITransformation
    { 
        public TransformationType Type => TransformationType.DropColumns;

        public HashSet<String> DropHeaderNames { get; set; } = new HashSet<String>();

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            var selected_fields =
                 from field in input_fields
                 where !DropHeaderNames.Contains(field.Header.Name)
                 select field;
            return selected_fields.ToList();
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            var selected_fields =
                 from field in list
                 where !DropHeaderNames.Contains(field.Header.Name)
                 select field;
            return selected_fields.ToList();
        }

        public string GetTransformationName() => "DropColumn";

        public string GetNextMovesInstructions() => "Select Field.Header.Name to drop";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var field in fields)
            {
                moves.Add(field.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Empty transformation has no arguments";

        public IEnumerable<string> GetArguments() => new List<string>();
    }

    public class SortByTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.SortBy;

        public SortDirection Direction { get; set; }

        public String SortByHeaderName { get; set; }

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            Field? source_field = input_fields.FirstOrDefault(x => x.Header.Name == this.SortByHeaderName);
            if (source_field is not null)
            {
                var indexes = source_field.SortAndGetIndexes(); // sort the given field and get the sorted indices
                return input_fields.ReArrangeAndSelectByIndex(source_field.Header, indexes); // re-arrange all fields by the sorted indices and ignore the one already sorted
            }
            else
            {
                throw new ArgumentException("Field to SortBy not found.");
            }
        }

        public List<EmptyField> Preprocess(List<EmptyField> list)
        {
            return list; // practically the set remains the same, only order changes
        }

        public string GetTransformationName() => "SortBy";

        public string GetNextMovesInstructions() => "Choose one Field from the list below, by which you want to sort the dataset";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var field in fields)
            {
                moves.Add(field.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Choose whether the sorting should be ascending or descending";

        public IEnumerable<string> GetArguments() => new List<string> { "Asc", "Desc" };
    }

    public class GroupByTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.GroupBy;

        public HashSet<string> StringsToGroup { get; set; }

        public Agregation GroupAgregation { get; set; }

        public String TargetHeaderName { get; set; }

        public List<Field> PerformTransformation(List<Field> fields)
        {
            Field? field = fields.FirstOrDefault(x => x.Header.Name == TargetHeaderName);
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
                int rowIndex = 0;
                switch (this.GroupAgregation)
                {
                    case Agregation.GroupKey:
                        throw new NotImplementedException();
                        break;

                    case Agregation.CountAll:
                        foreach (var group in grouped_cells)
                        {
                            int count = 0;
                            foreach (var item in group)
                            {
                                count += 1;
                            }
                            grouped.Add(new Cell() { Content = count.ToString(), Index = rowIndex});
                            rowIndex++;
                        }
                        field.Data = grouped;
                        field.Header.Type = FieldDataType.Number;
                        break;

                    case Agregation.CountDistinct:
                        HashSet<string> values = new();
                        foreach (var group in grouped_cells)
                        {
                            int count = 0;
                            foreach(var item in group)
                            {
                                if (!values.Contains(item.Content))
                                {
                                    count++;
                                    values.Add(item.Content);
                                }
                            }
                            grouped.Add(new Cell() { Content = count.ToString(), Index = rowIndex });
                            rowIndex++;
                        }
                        field.Data = grouped;
                        field.Header.Type = FieldDataType.Number;
                        break;

                    case Agregation.ConcatValues:
                        foreach (var group in grouped_cells)
                            grouped.Concat(group);
                        field.Data = grouped;
                        break;

                    case Agregation.Sum:
                        foreach (var group in grouped_cells)
                        {
                            double sum = 0;
                            foreach (var item in group)
                            {
                                if(Int32.TryParse(item.Content, out int num))
                                {
                                    sum += num;
                                }
                            }
                            grouped.Add(new Cell() { Content = sum.ToString(), Index = rowIndex});
                            rowIndex++;
                        }
                        field.Data = grouped;
                        field.Header.Type = FieldDataType.Number;
                        break;

                    case Agregation.Mean:
                        foreach (var group in grouped_cells)
                        {
                            double mean = 0;
                            uint count = 0;
                            foreach (var item in group)
                            {
                                if (Int32.TryParse(item.Content, out int num))
                                {
                                    mean = (mean * count / (count + 1)) + num / count + 1; // rolling average
                                    count++;
                                }
                            }
                            grouped.Add(new Cell() { Content= mean.ToString(), Index = rowIndex});
                            rowIndex++;
                        }
                        break;
                    default:
                        throw new ArgumentNullException("GroupAgregation in GroupBy transformation not set");
                }

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

        public string GetTransformationName() => "GroupBy";

        public string GetNextMovesInstructions() => "Choose one Field.Header.Name from the dataset, by which you want to group the dataset";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields) => new List<string> { "'any header name'" };

        public string GetArgumentsInstructions() => "Choose one of the following Agregations you want to apply on the grouped dataset";

        public IEnumerable<string> GetArguments() => new List<string> { "Sum", "Avg", "Concat", "CountDistinct", "CountAll", "GroupKey" };
    }

    public class FilterByTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.FilterBy;

        public FilterCondition FilterCondition { get; set; }

        public List<Field> PerformTransformation(List<Field> fields)
        {
            Field? field = fields.FirstOrDefault(x => x.Header.Name == FilterCondition.SourceHeaderName);
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

        public string GetTransformationName() => "FilterBy";

        public string GetNextMovesInstructions() => "Choose one Field.Header.Name from the dataset, by which you want to filter the dataset";

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields) 
        {
            var moves = new List<string>();
            foreach (var item in fields)
            {
                moves.Add(item.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Choose one of the following Relations you want to apply on the filtered dataset and fill the right side of the relation";

        public IEnumerable<string> GetArguments() => new List<string> { "==", "!=", "<", ">" };
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