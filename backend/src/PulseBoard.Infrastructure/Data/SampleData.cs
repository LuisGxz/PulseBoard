using System.Text.Json;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Infrastructure.Data;

/// <summary>
/// Deterministic generator for the bundled sample dataset — a SaaS product-analytics event stream
/// matching the dashboard mockup (revenue by plan/region, signups by channel, activity heatmap).
/// Seeded RNG so the demo is identical on every boot.
/// </summary>
public static class SampleData
{
    public record ColumnDef(string Name, string Label, ColumnType Type);

    public static readonly ColumnDef[] Columns =
    [
        new("event_date",  "Event date",   ColumnType.Date),
        new("event_hour",  "Hour of day",  ColumnType.Number),
        new("weekday",     "Weekday",      ColumnType.String),
        new("account",     "Account",      ColumnType.String),
        new("plan",        "Plan",         ColumnType.String),
        new("region",      "Region",       ColumnType.String),
        new("channel",     "Channel",      ColumnType.String),
        new("revenue_usd", "Revenue (USD)",ColumnType.Number),
        new("seats",       "Seats",        ColumnType.Number),
        new("is_paid",     "Paid",         ColumnType.Boolean),
    ];

    private static readonly (string Plan, double W, double Mrr)[] Plans =
        [("Free", 0.34, 0), ("Pro", 0.40, 49), ("Team", 0.20, 199), ("Enterprise", 0.06, 900)];

    private static readonly (string Region, double W)[] Regions =
        [("North America", 0.44), ("Europe", 0.30), ("APAC", 0.18), ("Other", 0.08)];

    private static readonly (string Channel, double W)[] Channels =
        [("Organic search", 0.41), ("Referral", 0.30), ("Paid social", 0.18), ("Newsletter", 0.11)];

    private static readonly string[] AccountNames =
    [
        "Northwind Labs","Karlsson & Co","Helio Systems","Bluegrain Analytics","Mercury Studio",
        "Atlas Freight","Pixelwerk GmbH","Faro Insurance","Cedar & Vale","Quanta Robotics",
        "Lumen Health","Drift Mobility","Granite Retail","Solstice Media","Vector Foods",
        "Harbor Capital","Nimbus Cloud","Orchard Logistics","Beacon Energy","Tideline Apparel",
    ];

    private static readonly string[] Weekdays = ["Mon","Tue","Wed","Thu","Fri","Sat","Sun"];

    /// <summary>Generates the event rows as JSON strings (one object per row), deterministic for the given day window.</summary>
    public static List<string> GenerateRows(DateOnly endDate, int days = 60, int approxRows = 5200)
    {
        var rng = new Random(20260618);
        var rows = new List<string>(approxRows);

        for (int i = 0; i < approxRows; i++)
        {
            // Date — weight recent days slightly heavier (growth curve)
            double dayBias = Math.Pow(rng.NextDouble(), 0.75);
            int dayOffset = (int)(dayBias * (days - 1));
            var date = endDate.AddDays(-dayOffset);
            var weekday = Weekdays[((int)date.DayOfWeek + 6) % 7];
            bool isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            // Hour — business-hours peak on weekdays
            int hour = SampleHour(rng, isWeekend);

            var (plan, _, mrr) = Pick(rng, Plans, x => x.W);
            var (region, _) = Pick(rng, Regions, x => x.W);
            var (channel, _) = Pick(rng, Channels, x => x.W);
            var account = AccountNames[rng.Next(AccountNames.Length)];

            bool isPaid = mrr > 0;
            int seats = plan switch
            {
                "Enterprise" => rng.Next(40, 160),
                "Team" => rng.Next(8, 40),
                "Pro" => rng.Next(2, 12),
                _ => 1,
            };
            // Revenue attributed to the event: paid plans contribute a slice of MRR with noise.
            double revenue = isPaid ? Math.Round(mrr * (0.5 + rng.NextDouble()) * (seats / 6.0 + 1), 2) : 0;

            var obj = new Dictionary<string, object?>
            {
                ["event_date"] = date.ToString("yyyy-MM-dd"),
                ["event_hour"] = hour,
                ["weekday"] = weekday,
                ["account"] = account,
                ["plan"] = plan,
                ["region"] = region,
                ["channel"] = channel,
                ["revenue_usd"] = revenue,
                ["seats"] = seats,
                ["is_paid"] = isPaid,
            };
            rows.Add(JsonSerializer.Serialize(obj));
        }
        return rows;
    }

    private static int SampleHour(Random rng, bool isWeekend)
    {
        // Triangular-ish distribution peaking around 10–16h on weekdays, flatter on weekends.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            int h = rng.Next(0, 24);
            double p = isWeekend
                ? 0.4 + 0.4 * Math.Exp(-Math.Pow(h - 14, 2) / 40.0)
                : (h is >= 9 and <= 18 ? 0.9 : 0.2) * (0.6 + 0.6 * Math.Exp(-Math.Pow(h - 13, 2) / 30.0));
            if (rng.NextDouble() < p) return h;
        }
        return rng.Next(9, 18);
    }

    private static (string, double, double) Pick(Random rng, (string Plan, double W, double Mrr)[] items, Func<(string Plan, double W, double Mrr), double> weight)
    {
        double r = rng.NextDouble(), acc = 0;
        foreach (var it in items) { acc += weight(it); if (r <= acc) return it; }
        return items[^1];
    }

    private static (string, double) Pick(Random rng, (string Name, double W)[] items, Func<(string Name, double W), double> weight)
    {
        double r = rng.NextDouble(), acc = 0;
        foreach (var it in items) { acc += weight(it); if (r <= acc) return it; }
        return items[^1];
    }
}
