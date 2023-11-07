using NaturalSQLParser.Types.Enums;
using System.Globalization;

namespace NaturalSQLParser.Types
{
    public class GroupAgregations
    {
        public Agregation Agregation { get; set; }

        public string Key { get; set; }
    }

    /// <summary>
    /// CSV file will be harvested by user requests.
    /// Therefore the structure will partially ignore the csv rows / columns and will create its own representation.
    /// Each column is specified by Header class, which is then shared throughout the whole <see cref="NaturalSQLParser.Types.Field"/> class.
    /// The <see cref="NaturalSQLParser.Types.Header"/> class also defines the column <see cref="FieldDataType", which is shared throughout the whole <see cref="NaturalSQLParser.Types.Field"/>.
    /// Field class consists of a List of atomic cells and a ref. to its parent Header.
    /// The list is supposed to simulate one column
    /// <see cref="NaturalSQLParser.Types.Cell"/> class is representing the raw data stored.
    /// When user wants to work with only some columns from a dataset, List<Field> is created, holding only the data selected.
    /// </summary>

    /// <summary>
    /// Type defines the ValueType of the whole column.
    /// Name defines the column name, usually given by the first row in CSV.
    /// Index defines the column index in the csv file.
    /// Used for shared representation of a column.
    /// </summary>
    public class Header
    {
        public FieldDataType Type { get; set; }

        public string Name { get; set; }

        public int Index { get; set; } = 0;

        public Header(string name)
        {
            Name = name;
        }

        public Header(string name, FieldDataType type)
        {
            Name = name;
            Type = type;
        }

        public Header(string name, FieldDataType type, int index)
        {
            Type = type;
            Name = name;
            Index = index;
        }
    }

    /// <summary>
    /// Atomic cell representing the raw data of a given type.
    /// Content is holding the data in string representation. (parsing is done only when necessary.
    /// Index defines on which row is the content stored in csv.
    /// </summary>
    public class Cell : IComparable<Cell>
    {
        public string Content { get; set; }

        public int Index { get; set; }

        public int CompareTo(Cell? other)
        {
            if (other is null)
                return -1;

            return String.Compare(this.Content, other.Content);
        }

        public int CompareToTypeDependent(FieldDataType type, Cell? other)
        {
            if (other is null)
                return -1;

            switch (type)
            {
                case FieldDataType.Bool:
                    bool A_Bool_parsingResult = Boolean.TryParse(this.Content, out bool A_bool);
                    bool B_Bool_parsingResult = Boolean.TryParse(other.Content, out bool B_bool);

                    if (A_Bool_parsingResult && B_Bool_parsingResult)
                    {
                        if (!A_bool && B_bool)
                            return -1;
                        else if (!A_bool && !B_bool)
                            return 0;
                        else if (A_bool && B_bool)
                            return 0;
                        else
                            return 1;
                    }
                    else if (A_Bool_parsingResult)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                case FieldDataType.String:
                    return String.Compare(this.Content, other.Content);

                case FieldDataType.Number:
                    bool A_parsingResult = Int32.TryParse(this.Content, out int A_number);
                    bool B_parsingResult = Int32.TryParse(other.Content, out int B_number);

                    if (A_parsingResult && B_parsingResult)
                    {
                        if (A_number < B_number)
                            return -1;
                        else if (A_number == B_number)
                            return 0;
                        else
                            return 1;
                    }
                    else if (A_parsingResult)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }

                case FieldDataType.Date:
                    bool A_DateTimeResult = DateTime.TryParse(this.Content, CultureInfo.CurrentCulture, out DateTime A_datetime);
                    bool B_DateTimeResult = DateTime.TryParse(other.Content, CultureInfo.CurrentCulture, out DateTime B_datetime);

                    if (A_DateTimeResult && B_DateTimeResult)
                    {
                        if (A_datetime < B_datetime)
                            return -1;
                        else if (A_datetime == B_datetime)
                            return 0;
                        else
                            return 1;
                    }
                    else if (A_DateTimeResult)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                default:
                    return 0;
            }
        }

        public int CompareTo_TypeDependent(FieldDataType type, string other)
        {
            if (other is null)
                return -1;

            switch (type)
            {
                case FieldDataType.Bool:
                    bool A_Bool_parsingResult = Boolean.TryParse(this.Content, out bool A_bool);
                    bool B_Bool_parsingResult = Boolean.TryParse(other, out bool B_bool);

                    if (A_Bool_parsingResult && B_Bool_parsingResult)
                    {
                        if (!A_bool && B_bool)
                            return -1;
                        else if (!A_bool && !B_bool)
                            return 0;
                        else if (A_bool && B_bool)
                            return 0;
                        else
                            return 1;
                    }
                    else if (A_Bool_parsingResult)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                case FieldDataType.String:
                    return String.Compare(this.Content, other);

                case FieldDataType.Number:
                    bool A_parsingResult = Int32.TryParse(this.Content, out int A_number);
                    bool B_parsingResult = Int32.TryParse(other, out int B_number);

                    if (A_parsingResult && B_parsingResult)
                    {
                        if (A_number < B_number)
                            return -1;
                        else if (A_number == B_number)
                            return 0;
                        else
                            return 1;
                    }
                    else if (A_parsingResult)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }

                case FieldDataType.Date:
                    bool A_DateTimeResult = DateTime.TryParse(this.Content, CultureInfo.InvariantCulture, out DateTime A_datetime);
                    bool B_DateTimeResult = DateTime.TryParse(other, CultureInfo.InvariantCulture, out DateTime B_datetime);

                    if (A_DateTimeResult && B_DateTimeResult)
                    {
                        if (A_datetime < B_datetime)
                            return -1;
                        else if (A_datetime == B_datetime)
                            return 0;
                        else
                            return 1;
                    }
                    else if (A_DateTimeResult)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// Full representation of a column from a CSV.
    /// <see cref="NaturalSQLParser.Types.Header"/> holds all header info (<see cref="FieldDataType"/> and Name).
    /// Data holds all individual cells from the given Collumn <see cref="NaturalSQLParser.Types.Cell"/>.
    /// </summary>
    
    public class EmptyField
    {
        public Header Header { get; set; }
    }

    public class Field : EmptyField
    {
        public List<Cell> Data { get; set; } = new();
    }

    public class DataSet
    {
        public List<Field> Fields { get; set; } = new();
    }

    public class FilterCondition
    {
        public Relation Relation { get; set; }

        public String SourceHeaderName { get; set; }

        public string Condition { get; set; }

        // if (source == condition)
    }
}
