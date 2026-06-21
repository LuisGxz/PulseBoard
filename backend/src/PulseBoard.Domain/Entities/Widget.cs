using PulseBoard.Domain.Common;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Domain.Entities;

/// <summary>
/// A single visualization on a dashboard. The query spec (metric, aggregation, group-by dimension,
/// date granularity, filters) is held in strongly-typed columns so the API can build the aggregation
/// without parsing free-form JSON. Grid position uses a 12-column layout.
/// </summary>
public class Widget : Entity
{
    public Guid DashboardId { get; set; }
    public Dashboard? Dashboard { get; set; }

    public WidgetType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }

    // Grid layout (12-col)
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridW { get; set; } = 4;
    public int GridH { get; set; } = 1;

    // Query spec
    public string? MetricColumn { get; set; }
    public Aggregation Aggregation { get; set; } = Aggregation.Sum;
    public string? DimensionColumn { get; set; }   // group-by (category or date)
    public string? SecondaryDimensionColumn { get; set; } // second group-by axis (heatmap matrix)
    public string? DateGranularity { get; set; }   // day | week | month (when dimension is a Date)
    public int? Limit { get; set; }                // top-N for bar/donut/table

    /// <summary>Optional JSON array of filter clauses: [{ "column", "op", "value" }]. Applied server-side.</summary>
    public string? FiltersJson { get; set; }
}
