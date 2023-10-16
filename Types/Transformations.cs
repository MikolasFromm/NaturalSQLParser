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
        public static IEnumerable<int> SortAndGetIndexes(this Field field, SortDirection sortDirection)
        {
            switch (sortDirection)
            {
                case SortDirection.Ascending:
                    field.Data = field.Data.OrderBy(x => x.Content).ToList();
                    break;
                case SortDirection.Descending:
                    field.Data = field.Data.OrderByDescending(x => x.Content).ToList();
                    break;
                default:
                    throw new ArgumentException("Unsupported sort direction");
            }

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
        public static ITransformation Create(string transformation)
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

        public static ITransformation CreateByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return new EmptyTransformation();
                case 1:
                    return new DropColumnTransformation();
                case 2:
                    return new SortByTransformation();
                case 3:
                    return new GroupByTransformation();
                case 4:
                    return new FilterByTransformation();
                default:
                    throw new ArgumentException("Unknown transformation");
            }
        }

        /// <summary>
        /// Transformation builder, which already expects all input arguments in given order and in corrent format.
        /// It is expected that user first calls <see cref="Create(string)"/> to obtain all moves and arguments.
        /// </summary>
        /// <param name="transformation"><see cref="String"/> representation of a transformation.</param>
        /// <param name="args">All arguments given for the transformation.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When wrong arguments given.</exception>
        public static ITransformation BuildTransformation(string transformation, params string[] args)
        {
            switch (transformation)
            {
                case StaticNames.Empty:
                case "0":
                    return new EmptyTransformation();

                case StaticNames.DropColumn:
                case "1":
                    if (args.Length < 1)
                        throw new ArgumentException("Not enough arguments for DropColumn transformation");

                    HashSet<string> dropColumns = new();
                    
                    foreach (var arg in args)
                    {
                        if (!string.IsNullOrEmpty(arg))
                            dropColumns.Add(arg);
                    }
                        
                    return new DropColumnTransformation(dropColumns);

                case StaticNames.SortBy:
                case "2":
                    if (args.Length < 2)
                        throw new ArgumentException("Not enough arguments for SortBy transformation");

                    SortDirection direction = SortDirection.Ascending;
                    string headerName = args[0]; // gets the header of the field by which to sort

                    if (args[1] == StaticNames.Ascending) // if ascending or descending
                        direction = SortDirection.Ascending;
                    else if (args[1] == StaticNames.Descending) // if ascending or descending
                        direction = SortDirection.Descending;
                    else
                        throw new ArgumentException($"SortDirection \"{args[1]}\" unrecognized");

                    return new SortByTransformation(headerName, direction);

                case StaticNames.GroupBy:
                case "3":
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for GroupBy transformation");

                    Agregation agregation = new();
                    string targetHeader = string.Empty;
                    HashSet<string> groups = new();


                    targetHeader = args[0]; // gets the header of the field by which to group
                    if (args[1] == StaticNames.Sum)
                        agregation = Agregation.Sum;
                    else if (args[1] == StaticNames.Average)
                        agregation = Agregation.Mean;
                    else if (args[1] == StaticNames.Concat)
                        agregation = Agregation.ConcatValues;
                    else if (args[1] == StaticNames.CountDistinct)
                        agregation = Agregation.CountDistinct;
                    else if (args[1] == StaticNames.CountAll)
                        agregation = Agregation.CountAll;
                    else if (args[1] == StaticNames.GroupKey)
                        agregation = Agregation.GroupKey;
                    else
                        throw new ArgumentException($"Agregation \"{args[1]}\" not supported");

                    for(int i = 2; i < args.Length; i++)
                        groups.Add(args[i]);

                    return new GroupByTransformation(groups, agregation, targetHeader);

                case StaticNames.FilterBy:
                case "4":
                    if (args.Length < 3)
                        throw new ArgumentException("Not enough arguments for FilterBy transformation");

                    FilterCondition filter = new();

                    filter.SourceHeaderName = args[0];

                    if (args[1] == StaticNames.Equals)
                    {
                        filter.Relation = Relation.Equals;
                        filter.Condition = args[2];
                    }
                    else if (args[1] == StaticNames.NotEquals)
                    {
                        filter.Relation = Relation.NotEquals;
                        filter.Condition = args[2];
                    }
                    else if (args[1] == StaticNames.LessThan)
                    {
                        filter.Relation = Relation.LessThan;
                        filter.Condition = args[2];
                    }
                    else if (args[1] == StaticNames.GreaterThan)
                    {
                        filter.Relation = Relation.GreaterThan;
                        filter.Condition = args[2];
                    }
                    else
                    {
                        throw new ArgumentException($"Operation \"{args[0]}\" not supported");
                    }
                    
                    return new FilterByTransformation(filter);

                default:
                    throw new ArgumentException($"Transformation \"{transformation}\" not supported");
            }
        }
    }

    public interface ITransformation
    {
        public TransformationType Type { get; }

        public bool HasArguments { get; }

        public bool HasFollowingHumanArguments { get; }

        public int TotalStepsNeeded { get; }

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

        /// <summary>
        /// Returns the argument at the given index. Allows the transformation class to adjust the following options based on the requested argument.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetArgumentAt(int index);

        public string GetFollowingHumanArgumentsInstructions();
    }

    public class EmptyTransformation : ITransformation
    {
        public TransformationType Type => TransformationType.Empty;

        public bool HasArguments => false;

        public bool HasFollowingHumanArguments => false;

        public int TotalStepsNeeded => 0;

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

        public string GetArgumentsInstructions() => string.Empty;

        public IEnumerable<string> GetArguments() => new List<string>();

        public string GetArgumentAt(int index) => string.Empty;

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;

    }

    public class DropColumnTransformation : ITransformation
    { 
        public TransformationType Type => TransformationType.DropColumns;

        public bool HasArguments => false;

        public bool HasFollowingHumanArguments => false;

        public int TotalStepsNeeded => 1;

        public HashSet<String> DropHeaderNames { get; set; } = new HashSet<String>();

        public DropColumnTransformation(HashSet<string> dropHeaderNames)
        {
            DropHeaderNames = dropHeaderNames;
        }

        internal DropColumnTransformation() { }

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

        public string GetArgumentsInstructions() => string.Empty;

        public IEnumerable<string> GetArguments() => new List<string>();

        public string GetArgumentAt(int index) => string.Empty;

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;
    }

    public class SortByTransformation : ITransformation
    {
        public bool HasArguments => true;

        public bool HasFollowingHumanArguments => false;

        public int TotalStepsNeeded => 2;

        public TransformationType Type => TransformationType.SortBy;

        public SortDirection Direction { get; set; }

        public String SortByHeaderName { get; set; }

        private static readonly List<string> _argumentsList = new List<string> { StaticNames.Ascending, StaticNames.Descending };

        public SortByTransformation(String sortByHeaderName, SortDirection direction)
        {
            SortByHeaderName = sortByHeaderName;
            Direction = direction;
        }

        internal SortByTransformation() { }

        public List<Field> PerformTransformation(List<Field> input_fields)
        {
            Field? source_field = input_fields.FirstOrDefault(x => x.Header.Name == this.SortByHeaderName);
            if (source_field is not null)
            {
                var indexes = source_field.SortAndGetIndexes(Direction); // sort the given field and get the sorted indices
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

        public IEnumerable<string> GetArguments() => _argumentsList;

        public string GetArgumentAt(int index)
        {
            if (index < 0 || index >= _argumentsList.Count)
                throw new ArgumentOutOfRangeException($"Index \"{index}\" in {nameof(GetArgumentAt)} out of range");
            return _argumentsList[index];
        }

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;
    }

    public class GroupByTransformation : ITransformation
    {
        public bool HasArguments => true;

        public bool HasFollowingHumanArguments { get; set; } // might be true if the arguments requires so

        public int TotalStepsNeeded => 3;

        public TransformationType Type => TransformationType.GroupBy;

        public HashSet<string> StringsToGroup { get; set; } = new HashSet<string>();

        public Agregation GroupAgregation { get; set; }

        public String TargetHeaderName { get; set; }

        private static readonly List<string> _argumentsList = new List<string> { StaticNames.Sum, StaticNames.Average, StaticNames.Concat, StaticNames.CountDistinct, StaticNames.CountAll, StaticNames.GroupKey };

        public GroupByTransformation(HashSet<string> stringsToGroup, Agregation groupAgregation, string targetHeaderName)
        {
            StringsToGroup = stringsToGroup;
            GroupAgregation = groupAgregation;
            TargetHeaderName = targetHeaderName;
            HasFollowingHumanArguments = false; // default value
        }

        internal GroupByTransformation() { }

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

        public IEnumerable<string> GetNextMoves(IEnumerable<EmptyField> fields)
        {
            var moves = new List<string>();
            foreach (var item in fields)
            {
                moves.Add(item.Header.Name);
            }
            return moves;
        }

        public string GetArgumentsInstructions() => "Choose one of the following Agregations you want to apply on the grouped dataset";

        public IEnumerable<string> GetArguments() => _argumentsList;

        public string GetArgumentAt(int index)
        {
            if (index < 0 || index >= _argumentsList.Count)
                throw new ArgumentOutOfRangeException($"Index \"{index}\" in {nameof(GetArgumentAt)} out of range");

            if (index == 0 // Sum
                || index == 1 // Avg
                || index == 2 // Concat
                || index == 4 // CountAll
                || index == 5) // GroupKey
            {
                HasFollowingHumanArguments = true;
            }

            return _argumentsList[index];
        }

        public string GetFollowingHumanArgumentsInstructions() => string.Empty;
    }

    public class FilterByTransformation : ITransformation
    {
        public bool HasArguments => true;

        public bool HasFollowingHumanArguments => true;

        public int TotalStepsNeeded => 3;

        public TransformationType Type => TransformationType.FilterBy;

        public FilterCondition FilterCondition { get; set; }

        private static readonly List<string> _argumentsList = new List<string> { StaticNames.Equals, StaticNames.NotEquals, StaticNames.LessThan, StaticNames.GreaterThan };

        public FilterByTransformation(FilterCondition filterCondition)
        {
            FilterCondition = filterCondition;
        }

        internal FilterByTransformation() { }

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

        public string GetArgumentsInstructions() => "Choose one of the following Relations you want to apply on the filtered dataset";

        public IEnumerable<string> GetArguments() => _argumentsList;

        public string GetArgumentAt(int index)
        {
            if (index < 0 || index >= _argumentsList.Count)
                throw new ArgumentOutOfRangeException($"Index \"{index}\" in {nameof(GetArgumentAt)} out of range");
            return _argumentsList[index];
        }

        public string GetFollowingHumanArgumentsInstructions() => "Write down the right side of the relation.";
    }

    public static class Transformator
    {
        public static List<Field> TransformFields(List<Field> fields, IEnumerable<ITransformation> transformations)
        {
            foreach (var transformation in transformations)
            {
                fields = transformation.PerformTransformation(fields);
            }
            return fields;
        }
    }
}