using NaturalSQLParser.Types.Enums;

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
