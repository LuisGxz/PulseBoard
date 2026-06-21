using PulseBoard.Application.Features.Analytics;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Widgets;

public record WidgetDto(
    Guid Id, string Type, string Title, int Position,
    int GridX, int GridY, int GridW, int GridH,
    string? MetricColumn, string Aggregation, string? DimensionColumn, string? SecondaryDimensionColumn,
    string? DateGranularity, int? Limit, string? FiltersJson)
{
    public static WidgetDto From(Widget w) => new(
        w.Id, w.Type.ToString(), w.Title, w.Position,
        w.GridX, w.GridY, w.GridW, w.GridH,
        w.MetricColumn, w.Aggregation.ToString(), w.DimensionColumn, w.SecondaryDimensionColumn,
        w.DateGranularity, w.Limit, w.FiltersJson);
}

/// <summary>A widget plus its computed query result. <c>Error</c> is set if that widget's query failed,
/// so one misconfigured widget never breaks the whole dashboard load.</summary>
public record WidgetWithDataDto(WidgetDto Widget, QueryResult? Data, string? Error);

/// <summary>Create/update payload for a widget. Type and Aggregation are enum names.</summary>
public record SaveWidgetRequest(
    string Type, string Title,
    int GridX, int GridY, int GridW, int GridH,
    string? MetricColumn, string Aggregation, string? DimensionColumn, string? SecondaryDimensionColumn,
    string? DateGranularity, int? Limit, string? FiltersJson);

/// <summary>Builds an aggregation spec from a saved widget, with an optional global date range overlay.</summary>
public static class WidgetSpecFactory
{
    public static AggregationSpec From(Widget w, Guid datasetId, string? dateColumn = null, DateOnly? from = null, DateOnly? to = null) =>
        new()
        {
            DatasetId = datasetId,
            MetricColumn = w.MetricColumn,
            Aggregation = w.Aggregation,
            DimensionColumn = w.DimensionColumn,
            SecondaryDimensionColumn = w.SecondaryDimensionColumn,
            DateGranularity = w.DateGranularity,
            Limit = w.Limit,
            FiltersJson = w.FiltersJson,
            DateColumn = dateColumn,
            From = from,
            To = to,
        };
}
