using System.Text.Json;
using System.Text.RegularExpressions;
using PulseBoard.Application.Features.Analytics;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Infrastructure.Analytics;

public sealed record QueryParam(string Name, object? Value);

public sealed record BuiltQuery(string Sql, IReadOnlyList<QueryParam> Parameters);

/// <summary>
/// Translates an <see cref="AggregationSpec"/> into a parameterized SQL query over <c>dataset_rows.Data</c> (JSONB).
/// Pure and DB-free so it is fully unit-testable. Every referenced column is validated against the dataset's known
/// columns and against a strict name pattern, so the interpolated identifiers cannot carry SQL injection.
/// </summary>
public static partial class AggregationSqlBuilder
{
    private static readonly HashSet<string> Granularities = new(StringComparer.OrdinalIgnoreCase) { "day", "week", "month" };
    private const int MaxFilters = 25;

    [GeneratedRegex("^[a-z0-9_]+$")]
    private static partial Regex SafeNameRegex();

    public static BuiltQuery Build(AggregationSpec spec, IReadOnlyDictionary<string, ColumnType> columns)
    {
        var p = new List<QueryParam> { new("datasetId", spec.DatasetId) };
        var where = new List<string> { "\"DatasetId\" = @datasetId" };

        if (!string.IsNullOrWhiteSpace(spec.DateColumn) && (spec.From is not null || spec.To is not null))
        {
            var col = Resolve(spec.DateColumn!, columns);
            if (spec.From is { } from) { where.Add($"({Json(col)})::date >= @from"); p.Add(new("from", from)); }
            if (spec.To is { } to) { where.Add($"({Json(col)})::date <= @to"); p.Add(new("to", to)); }
        }

        AppendFilters(spec.FiltersJson, columns, where, p);

        var whereSql = string.Join(" AND ", where);
        var agg = AggregationExpr(spec, columns);

        var hasSecondary = !string.IsNullOrWhiteSpace(spec.SecondaryDimensionColumn);
        var hasPrimary = !string.IsNullOrWhiteSpace(spec.DimensionColumn);

        if (hasSecondary && hasPrimary)
        {
            var (d1, _) = DimensionExpr(spec.DimensionColumn!, spec.DateGranularity, columns);
            var (d2, _) = DimensionExpr(spec.SecondaryDimensionColumn!, null, columns);
            return new(
                $"SELECT {d1} AS x, {d2} AS y, {agg} AS v FROM dataset_rows WHERE {whereSql} GROUP BY x, y ORDER BY x, y",
                p);
        }

        if (hasPrimary)
        {
            var (dim, isChronological) = DimensionExpr(spec.DimensionColumn!, spec.DateGranularity, columns);
            var order = isChronological ? "ORDER BY k ASC" : "ORDER BY v DESC NULLS LAST, k ASC";
            var limit = "";
            if (!isChronological && spec.Limit is int n and > 0)
            {
                limit = " LIMIT @limit";
                p.Add(new("limit", n));
            }
            return new($"SELECT {dim} AS k, {agg} AS v FROM dataset_rows WHERE {whereSql} GROUP BY k {order}{limit}", p);
        }

        return new($"SELECT {agg} AS v FROM dataset_rows WHERE {whereSql}", p);
    }

    /// <summary>Paginated raw-row query for the dataset table view (filters reused from the same parser).</summary>
    public static BuiltQuery BuildRows(
        Guid datasetId, string? filtersJson, IReadOnlyDictionary<string, ColumnType> columns, int limit, int offset)
    {
        var p = new List<QueryParam> { new("datasetId", datasetId), new("lim", limit), new("off", offset) };
        var where = new List<string> { "\"DatasetId\" = @datasetId" };
        AppendFilters(filtersJson, columns, where, p);
        var sql = $"SELECT \"Data\" FROM dataset_rows WHERE {string.Join(" AND ", where)} " +
                  "ORDER BY \"CreatedAt\" LIMIT @lim OFFSET @off";
        return new(sql, p);
    }

    public static BuiltQuery BuildRowCount(
        Guid datasetId, string? filtersJson, IReadOnlyDictionary<string, ColumnType> columns)
    {
        var p = new List<QueryParam> { new("datasetId", datasetId) };
        var where = new List<string> { "\"DatasetId\" = @datasetId" };
        AppendFilters(filtersJson, columns, where, p);
        return new($"SELECT COUNT(*) FROM dataset_rows WHERE {string.Join(" AND ", where)}", p);
    }

    private static string Json(string col) => $"\"Data\"->>'{col}'";

    private static string Resolve(string name, IReadOnlyDictionary<string, ColumnType> columns)
    {
        if (string.IsNullOrWhiteSpace(name) || !SafeNameRegex().IsMatch(name))
            throw new InvalidOperationException($"Invalid column name '{name}'.");
        if (!columns.ContainsKey(name))
            throw new InvalidOperationException($"Unknown column '{name}' for this dataset.");
        return name;
    }

    private static (string Expr, bool Chronological) DimensionExpr(
        string col, string? granularity, IReadOnlyDictionary<string, ColumnType> columns)
    {
        var c = Resolve(col, columns);
        if (!string.IsNullOrWhiteSpace(granularity))
        {
            if (!Granularities.Contains(granularity))
                throw new InvalidOperationException($"Invalid date granularity '{granularity}'.");
            return ($"to_char(date_trunc('{granularity.ToLowerInvariant()}', ({Json(c)})::date), 'YYYY-MM-DD')", true);
        }
        return ($"({Json(c)})", columns[c] == ColumnType.Date);
    }

    private static string AggregationExpr(AggregationSpec spec, IReadOnlyDictionary<string, ColumnType> columns)
    {
        switch (spec.Aggregation)
        {
            case Aggregation.Count:
                return "COUNT(*)";
            case Aggregation.CountDistinct:
                return $"COUNT(DISTINCT ({Json(RequireMetric(spec, columns))}))";
            default:
                var metric = NumericExpr(RequireMetric(spec, columns), columns);
                var fn = spec.Aggregation switch
                {
                    Aggregation.Sum => "SUM",
                    Aggregation.Avg => "AVG",
                    Aggregation.Min => "MIN",
                    Aggregation.Max => "MAX",
                    _ => throw new InvalidOperationException($"Unsupported aggregation '{spec.Aggregation}'."),
                };
                return $"{fn}({metric})";
        }
    }

    private static string RequireMetric(AggregationSpec spec, IReadOnlyDictionary<string, ColumnType> columns)
    {
        if (string.IsNullOrWhiteSpace(spec.MetricColumn))
            throw new InvalidOperationException($"Aggregation '{spec.Aggregation}' requires a metric column.");
        return Resolve(spec.MetricColumn, columns);
    }

    private static string NumericExpr(string col, IReadOnlyDictionary<string, ColumnType> columns) =>
        columns[col] switch
        {
            ColumnType.Number => $"({Json(col)})::numeric",
            ColumnType.Boolean => $"(({Json(col)})::boolean)::int::numeric",
            _ => throw new InvalidOperationException($"Column '{col}' is not numeric; cannot apply this aggregation."),
        };

    private static string CompareExpr(string col, ColumnType type) => type switch
    {
        ColumnType.Number => $"({Json(col)})::numeric",
        ColumnType.Date => $"({Json(col)})::date",
        ColumnType.Boolean => $"({Json(col)})::boolean",
        _ => $"({Json(col)})",
    };

    private static void AppendFilters(
        string? filtersJson, IReadOnlyDictionary<string, ColumnType> columns, List<string> where, List<QueryParam> p)
    {
        if (string.IsNullOrWhiteSpace(filtersJson))
            return;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(filtersJson); }
        catch (JsonException) { throw new InvalidOperationException("Filters must be valid JSON."); }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Filters must be a JSON array.");

            var idx = 0;
            foreach (var f in doc.RootElement.EnumerateArray())
            {
                if (idx >= MaxFilters)
                    throw new InvalidOperationException($"Too many filters (max {MaxFilters}).");

                var col = Resolve(GetString(f, "column"), columns);
                var op = GetString(f, "op").ToLowerInvariant();
                var value = f.GetProperty("value");
                var type = columns[col];
                var name = $"f{idx++}";

                switch (op)
                {
                    case "eq": where.Add($"{CompareExpr(col, type)} = @{name}"); p.Add(new(name, Typed(value, type))); break;
                    case "ne": where.Add($"{CompareExpr(col, type)} <> @{name}"); p.Add(new(name, Typed(value, type))); break;
                    case "gt": where.Add($"{CompareExpr(col, type)} > @{name}"); p.Add(new(name, Typed(value, type))); break;
                    case "gte": where.Add($"{CompareExpr(col, type)} >= @{name}"); p.Add(new(name, Typed(value, type))); break;
                    case "lt": where.Add($"{CompareExpr(col, type)} < @{name}"); p.Add(new(name, Typed(value, type))); break;
                    case "lte": where.Add($"{CompareExpr(col, type)} <= @{name}"); p.Add(new(name, Typed(value, type))); break;
                    case "contains": where.Add($"({Json(col)}) ILIKE @{name}"); p.Add(new(name, $"%{AsText(value)}%")); break;
                    case "in":
                        if (value.ValueKind != JsonValueKind.Array)
                            throw new InvalidOperationException("'in' filter requires an array value.");
                        var items = value.EnumerateArray().Select(AsText).ToArray();
                        where.Add($"({Json(col)}) = ANY(@{name})");
                        p.Add(new(name, items));
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported filter operator '{op}'.");
                }
            }
        }
    }

    private static string GetString(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : throw new InvalidOperationException($"Filter is missing string property '{prop}'.");

    private static string AsText(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString()!,
        JsonValueKind.Number => e.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => e.GetRawText(),
    };

    private static object Typed(JsonElement value, ColumnType type) => type switch
    {
        ColumnType.Number => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.Parse(AsText(value)),
        ColumnType.Date => DateOnly.Parse(AsText(value)),
        ColumnType.Boolean => value.ValueKind == JsonValueKind.True ||
                              (value.ValueKind == JsonValueKind.String && bool.Parse(value.GetString()!)),
        _ => AsText(value),
    };
}
