using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Analytics;

/// <summary>
/// A self-contained aggregation request over a dataset's JSONB rows. Built from a saved
/// <c>Widget</c> or posted ad-hoc by the dashboard builder for live preview.
/// </summary>
public record AggregationSpec
{
    public Guid DatasetId { get; init; }
    public string? MetricColumn { get; init; }
    public Aggregation Aggregation { get; init; } = Aggregation.Count;
    public string? DimensionColumn { get; init; }
    public string? SecondaryDimensionColumn { get; init; }
    public string? DateGranularity { get; init; } // day | week | month
    public int? Limit { get; init; }
    public string? FiltersJson { get; init; }

    // Optional global date-range filter (drill-down / dashboard date picker).
    public string? DateColumn { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public record SeriesPoint(string Key, double Value);

public record MatrixCell(string X, string Y, double Value);

/// <summary>
/// Result shape adapts to the spec: <c>scalar</c> (KPI, no dimension), <c>series</c> (one dimension),
/// or <c>matrix</c> (two dimensions, heatmap).
/// </summary>
public record QueryResult(string Kind, double? Scalar, IReadOnlyList<SeriesPoint>? Points, IReadOnlyList<MatrixCell>? Matrix)
{
    public static QueryResult OfScalar(double? value) => new("scalar", value, null, null);
    public static QueryResult OfSeries(IReadOnlyList<SeriesPoint> points) => new("series", null, points, null);
    public static QueryResult OfMatrix(IReadOnlyList<MatrixCell> cells) => new("matrix", null, null, cells);
}
