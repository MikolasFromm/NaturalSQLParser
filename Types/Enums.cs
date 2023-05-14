namespace NaturalSQLParser.Types.Enums
{
    public enum FieldDataType
    {
        Bool,
        String,
        Number,
        Date
    };

    public enum Selection
    {
        Take,
        Skip
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum FilterLogicalOperator
    {
        And,
        Or
    }

    public enum Relation
    {
        Equals,
        NotEquals,
        LessThan,
        GreaterThan,
        InRange
    }

    public enum Agregation
    {
        GroupKey,
        CountAll,
        CountDistinct,
        ConcatValues,
        Sum,
        Mean
    }

    public enum TransformationType
    {
        DropColumns,
        SortBy,
        Paging,
        GroupBy,
        FilterBy,
        Aggregate,
        Empty
    }

    public enum CommunicationAgentMode
    {
        User,
        AIBot
    }
}