using System.Text.Json;
using PulseBoard.Domain.Entities;

namespace PulseBoard.Application.Features.Datasets;

public record DatasetSummaryDto(
    Guid Id, string Name, string Slug, string Description, string Status,
    long RowCount, int ColumnCount, DateTimeOffset UpdatedAt);

public record DatasetColumnDto(
    string Name, string Label, string Type, int Position,
    long NullCount, long DistinctCount, double? MinNumeric, double? MaxNumeric,
    IReadOnlyList<string> SampleValues)
{
    public static DatasetColumnDto From(DatasetColumn c) => new(
        c.Name, c.Label, c.Type.ToString(), c.Position, c.NullCount, c.DistinctCount,
        c.MinNumeric, c.MaxNumeric, ParseSamples(c.SampleValues));

    private static IReadOnlyList<string> ParseSamples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}

public record DatasetDetailDto(
    Guid Id, string Name, string Slug, string Description, string Status, long RowCount,
    IReadOnlyList<DatasetColumnDto> Columns);

public record DatasetRowsDto(
    long Total, int Page, int PageSize,
    IReadOnlyList<DatasetColumnDto> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);
