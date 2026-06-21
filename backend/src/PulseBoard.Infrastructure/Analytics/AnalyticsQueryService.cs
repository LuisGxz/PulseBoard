using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Application.Features.Analytics;
using PulseBoard.Domain.Enums;
using PulseBoard.Infrastructure.Data;

namespace PulseBoard.Infrastructure.Analytics;

/// <summary>
/// Runs the SQL produced by <see cref="AggregationSqlBuilder"/> against Postgres. Column types are loaded
/// from <c>dataset_columns</c> so the builder can pick the right JSONB casts and reject unknown columns.
/// </summary>
public class AnalyticsQueryService(PulseBoardDbContext db) : IAnalyticsQueryService
{
    public async Task<QueryResult> RunAsync(AggregationSpec spec, CancellationToken ct = default)
    {
        var columns = await LoadColumnsAsync(spec.DatasetId, ct);
        var built = AggregationSqlBuilder.Build(spec, columns);

        var hasSecondary = !string.IsNullOrWhiteSpace(spec.SecondaryDimensionColumn);
        var hasPrimary = !string.IsNullOrWhiteSpace(spec.DimensionColumn);

        await using var cmd = CreateCommand(built);
        await OpenAsync(ct);

        if (hasSecondary && hasPrimary)
        {
            var cells = new List<MatrixCell>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                cells.Add(new MatrixCell(AsKey(reader.GetValue(0)), AsKey(reader.GetValue(1)), AsDouble(reader.GetValue(2))));
            return QueryResult.OfMatrix(cells);
        }

        if (hasPrimary)
        {
            var points = new List<SeriesPoint>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                points.Add(new SeriesPoint(AsKey(reader.GetValue(0)), AsDouble(reader.GetValue(1))));
            return QueryResult.OfSeries(points);
        }

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return QueryResult.OfScalar(scalar is null or DBNull ? null : AsDouble(scalar));
    }

    public async Task<(IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows, long Total)> GetRowsAsync(
        Guid datasetId, int page, int pageSize, string? filtersJson, CancellationToken ct = default)
    {
        var columns = await LoadColumnsAsync(datasetId, ct);
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(page, 1);

        await OpenAsync(ct);

        var countQuery = AggregationSqlBuilder.BuildRowCount(datasetId, filtersJson, columns);
        long total;
        await using (var countCmd = CreateCommand(countQuery))
            total = Convert.ToInt64(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        var rowsQuery = AggregationSqlBuilder.BuildRows(datasetId, filtersJson, columns, pageSize, (page - 1) * pageSize);
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        await using (var rowsCmd = CreateCommand(rowsQuery))
        await using (var reader = await rowsCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var json = reader.GetString(0);
                using var doc = JsonDocument.Parse(json);
                var map = new Dictionary<string, object?>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                    map[prop.Name] = JsonValue(prop.Value);
                rows.Add(map);
            }
        }

        return (rows, total);
    }

    private async Task<Dictionary<string, ColumnType>> LoadColumnsAsync(Guid datasetId, CancellationToken ct)
    {
        var columns = await db.DatasetColumns
            .Where(c => c.DatasetId == datasetId)
            .Select(c => new { c.Name, c.Type })
            .ToListAsync(ct);

        if (columns.Count == 0)
            throw new Application.Common.Exceptions.NotFoundException("Dataset", datasetId);

        return columns.ToDictionary(c => c.Name, c => c.Type);
    }

    private DbCommand CreateCommand(BuiltQuery built)
    {
        var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = built.Sql;
        foreach (var prm in built.Parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = prm.Name;
            p.Value = prm.Value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        return cmd;
    }

    private async Task OpenAsync(CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);
    }

    private static string AsKey(object? value) => value is null or DBNull ? "" : value.ToString() ?? "";

    private static double AsDouble(object value) => value switch
    {
        DBNull => 0,
        long l => l,
        int i => i,
        decimal m => (double)m,
        double d => d,
        _ => Convert.ToDouble(value),
    };

    private static object? JsonValue(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => e.GetString(),
    };
}
