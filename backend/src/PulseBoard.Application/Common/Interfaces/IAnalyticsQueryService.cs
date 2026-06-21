using PulseBoard.Application.Features.Analytics;

namespace PulseBoard.Application.Common.Interfaces;

/// <summary>Runs aggregation queries over a dataset's JSONB rows (group-by, filters, date ranges, top-N).</summary>
public interface IAnalyticsQueryService
{
    Task<QueryResult> RunAsync(AggregationSpec spec, CancellationToken ct = default);

    /// <summary>Returns a page of raw dataset rows (each row is a column→value map) plus the total count.</summary>
    Task<(IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows, long Total)> GetRowsAsync(
        Guid datasetId, int page, int pageSize, string? filtersJson, CancellationToken ct = default);
}
