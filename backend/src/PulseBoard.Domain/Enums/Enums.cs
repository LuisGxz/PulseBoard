namespace PulseBoard.Domain.Enums;

/// <summary>Global application role. Dashboard-level access is governed separately by <see cref="DashboardRole"/>.</summary>
public enum AppRole
{
    Admin = 0,
    Member = 1,
}

/// <summary>Per-dashboard collaboration role (RBAC). Owner > Editor > Viewer.</summary>
public enum DashboardRole
{
    Viewer = 0,
    Editor = 1,
    Owner = 2,
}

public enum DatasetSource
{
    CsvUpload = 0,
    Sample = 1,
}

public enum DatasetStatus
{
    Processing = 0,
    Ready = 1,
    Failed = 2,
}

/// <summary>Inferred logical type of a dataset column, used to drive aggregation and charting.</summary>
public enum ColumnType
{
    String = 0,
    Number = 1,
    Date = 2,
    Boolean = 3,
}

public enum WidgetType
{
    Kpi = 0,
    Line = 1,
    Bar = 2,
    Donut = 3,
    Heatmap = 4,
    Table = 5,
}

public enum Aggregation
{
    Sum = 0,
    Avg = 1,
    Count = 2,
    CountDistinct = 3,
    Min = 4,
    Max = 5,
}
