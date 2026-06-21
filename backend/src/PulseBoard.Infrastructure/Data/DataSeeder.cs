using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Infrastructure.Data;

/// <summary>Idempotent demo seed: 3 role accounts, one rich sample dataset, and a pre-built dashboard.</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(PulseBoardDbContext db, DateOnly today, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return;

        // ── Users ─────────────────────────────────────────────────────────────
        var admin  = NewUser("admin@pulseboard.io",  "Ada Lin (Admin)",   "Admin123!",  AppRole.Admin);
        var editor = NewUser("editor@pulseboard.io", "Theo Marsh (Editor)", "Editor123!", AppRole.Member);
        var viewer = NewUser("viewer@pulseboard.io", "Mara Vox (Viewer)", "Viewer123!", AppRole.Member);
        db.Users.AddRange(admin, editor, viewer);

        // ── Sample dataset ────────────────────────────────────────────────────
        var dataset = new Dataset
        {
            Name = "SaaS Product Analytics",
            Slug = "saas-product-analytics",
            Description = "Synthetic product event stream: revenue, plans, regions, channels and activity by hour. Bundled sample for the demo.",
            Source = DatasetSource.Sample,
            Status = DatasetStatus.Ready,
            OwnerId = admin.Id,
        };
        var columns = SampleData.Columns.Select((c, i) => new DatasetColumn
        {
            DatasetId = dataset.Id, Name = c.Name, Label = c.Label, Type = c.Type, Position = i,
        }).ToList();
        dataset.Columns = columns;

        var rowJson = SampleData.GenerateRows(today);
        dataset.RowCount = rowJson.Count;
        ProfileColumns(columns, rowJson);

        db.Datasets.Add(dataset);
        db.DataRows.AddRange(rowJson.Select(j => new DataRow { DatasetId = dataset.Id, Data = j }));

        // ── Sample dashboard ──────────────────────────────────────────────────
        var dash = new Dashboard
        {
            Name = "Product revenue · Q2",
            Slug = "product-revenue-q2",
            Description = "Headline revenue, regional split, acquisition channels and activity patterns.",
            DatasetId = dataset.Id,
            OwnerId = admin.Id,
        };
        dash.Members =
        [
            new DashboardMember { DashboardId = dash.Id, UserId = admin.Id,  Role = DashboardRole.Owner },
            new DashboardMember { DashboardId = dash.Id, UserId = editor.Id, Role = DashboardRole.Editor },
            new DashboardMember { DashboardId = dash.Id, UserId = viewer.Id, Role = DashboardRole.Viewer },
        ];
        dash.Widgets =
        [
            Kpi(dash.Id, "Revenue", "revenue_usd", Aggregation.Sum,        0, 0, 0),
            Kpi(dash.Id, "Events", null,           Aggregation.Count,      1, 3, 0),
            Kpi(dash.Id, "Paid conversion", "is_paid", Aggregation.Avg,    2, 6, 0),
            Kpi(dash.Id, "Avg seats", "seats",     Aggregation.Avg,        3, 9, 0),
            new Widget { DashboardId = dash.Id, Type = WidgetType.Line,  Title = "Daily revenue", Position = 4,
                         MetricColumn = "revenue_usd", Aggregation = Aggregation.Sum, DimensionColumn = "event_date", DateGranularity = "day",
                         GridX = 0, GridY = 1, GridW = 8, GridH = 2 },
            new Widget { DashboardId = dash.Id, Type = WidgetType.Donut, Title = "Revenue by region", Position = 5,
                         MetricColumn = "revenue_usd", Aggregation = Aggregation.Sum, DimensionColumn = "region",
                         GridX = 8, GridY = 1, GridW = 4, GridH = 2 },
            new Widget { DashboardId = dash.Id, Type = WidgetType.Bar,   Title = "Events by channel", Position = 6,
                         Aggregation = Aggregation.Count, DimensionColumn = "channel", Limit = 6,
                         GridX = 8, GridY = 3, GridW = 4, GridH = 2 },
            new Widget { DashboardId = dash.Id, Type = WidgetType.Heatmap, Title = "Activity by weekday & hour", Position = 7,
                         Aggregation = Aggregation.Count, DimensionColumn = "weekday", SecondaryDimensionColumn = "event_hour",
                         GridX = 0, GridY = 3, GridW = 8, GridH = 2 },
        ];
        db.Dashboards.Add(dash);

        await db.SaveChangesAsync(ct);
    }

    private static User NewUser(string email, string name, string pwd, AppRole role) => new()
    {
        Email = email, DisplayName = name, Role = role,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd),
    };

    private static Widget Kpi(Guid dashId, string title, string? metric, Aggregation agg, int pos, int x, int y) => new()
    {
        DashboardId = dashId, Type = WidgetType.Kpi, Title = title, Position = pos,
        MetricColumn = metric, Aggregation = agg, GridX = x, GridY = y, GridW = 3, GridH = 1,
    };

    /// <summary>Computes null/distinct counts, numeric min/max and a few sample values per column.</summary>
    private static void ProfileColumns(List<DatasetColumn> columns, List<string> rowJson)
    {
        var distinct = columns.ToDictionary(c => c.Name, _ => new HashSet<string>());
        var nulls = columns.ToDictionary(c => c.Name, _ => 0L);
        var min = columns.ToDictionary(c => c.Name, _ => (double?)null);
        var max = columns.ToDictionary(c => c.Name, _ => (double?)null);

        foreach (var j in rowJson)
        {
            using var doc = JsonDocument.Parse(j);
            foreach (var c in columns)
            {
                if (!doc.RootElement.TryGetProperty(c.Name, out var v) || v.ValueKind is JsonValueKind.Null)
                { nulls[c.Name]++; continue; }

                distinct[c.Name].Add(v.ToString());
                if (c.Type == ColumnType.Number && v.ValueKind == JsonValueKind.Number)
                {
                    var d = v.GetDouble();
                    min[c.Name] = min[c.Name] is { } lo ? Math.Min(lo, d) : d;
                    max[c.Name] = max[c.Name] is { } hi ? Math.Max(hi, d) : d;
                }
            }
        }

        foreach (var c in columns)
        {
            c.NullCount = nulls[c.Name];
            c.DistinctCount = distinct[c.Name].Count;
            c.MinNumeric = min[c.Name];
            c.MaxNumeric = max[c.Name];
            c.SampleValues = JsonSerializer.Serialize(distinct[c.Name].Take(8).ToArray());
        }
    }
}
