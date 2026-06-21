using PulseBoard.Application.Features.Analytics;
using PulseBoard.Domain.Enums;
using PulseBoard.Infrastructure.Analytics;

namespace PulseBoard.Tests;

public class AggregationSqlBuilderTests
{
    private static readonly Dictionary<string, ColumnType> Columns = new()
    {
        ["revenue_usd"] = ColumnType.Number,
        ["seats"] = ColumnType.Number,
        ["is_paid"] = ColumnType.Boolean,
        ["region"] = ColumnType.String,
        ["plan"] = ColumnType.String,
        ["event_date"] = ColumnType.Date,
        ["event_hour"] = ColumnType.Number,
        ["weekday"] = ColumnType.String,
    };

    private static readonly Guid Ds = Guid.NewGuid();

    [Fact]
    public void Scalar_Count_HasNoGroupBy()
    {
        var q = AggregationSqlBuilder.Build(
            new AggregationSpec { DatasetId = Ds, Aggregation = Aggregation.Count }, Columns);

        Assert.Contains("COUNT(*)", q.Sql);
        Assert.DoesNotContain("GROUP BY", q.Sql);
        Assert.Equal(Ds, q.Parameters.Single(p => p.Name == "datasetId").Value);
    }

    [Fact]
    public void Sum_OnNumber_CastsToNumeric()
    {
        var q = AggregationSqlBuilder.Build(
            new AggregationSpec { DatasetId = Ds, Aggregation = Aggregation.Sum, MetricColumn = "revenue_usd" }, Columns);

        Assert.Contains("SUM((\"Data\"->>'revenue_usd')::numeric)", q.Sql);
    }

    [Fact]
    public void Avg_OnBoolean_CastsBoolToInt()
    {
        var q = AggregationSqlBuilder.Build(
            new AggregationSpec { DatasetId = Ds, Aggregation = Aggregation.Avg, MetricColumn = "is_paid" }, Columns);

        Assert.Contains("AVG(((\"Data\"->>'is_paid')::boolean)::int::numeric)", q.Sql);
    }

    [Fact]
    public void CategoricalSeries_OrdersByValueDesc_AndLimits()
    {
        var q = AggregationSqlBuilder.Build(new AggregationSpec
        {
            DatasetId = Ds, Aggregation = Aggregation.Count, DimensionColumn = "region", Limit = 5,
        }, Columns);

        Assert.Contains("GROUP BY k", q.Sql);
        Assert.Contains("ORDER BY v DESC", q.Sql);
        Assert.Contains("LIMIT @limit", q.Sql);
        Assert.Equal(5, q.Parameters.Single(p => p.Name == "limit").Value);
    }

    [Fact]
    public void DateSeries_UsesDateTrunc_OrdersChronologically_IgnoresLimit()
    {
        var q = AggregationSqlBuilder.Build(new AggregationSpec
        {
            DatasetId = Ds, Aggregation = Aggregation.Sum, MetricColumn = "revenue_usd",
            DimensionColumn = "event_date", DateGranularity = "month", Limit = 5,
        }, Columns);

        Assert.Contains("date_trunc('month'", q.Sql);
        Assert.Contains("ORDER BY k ASC", q.Sql);
        Assert.DoesNotContain("LIMIT", q.Sql);
    }

    [Fact]
    public void TwoDimensions_ProduceMatrixQuery()
    {
        var q = AggregationSqlBuilder.Build(new AggregationSpec
        {
            DatasetId = Ds, Aggregation = Aggregation.Count,
            DimensionColumn = "weekday", SecondaryDimensionColumn = "event_hour",
        }, Columns);

        Assert.Contains(" AS x,", q.Sql);
        Assert.Contains(" AS y,", q.Sql);
        Assert.Contains("GROUP BY x, y", q.Sql);
    }

    [Fact]
    public void Filters_Eq_Contains_In_AddClausesAndParams()
    {
        var filters = """
        [{"column":"region","op":"eq","value":"Europe"},
         {"column":"plan","op":"contains","value":"Ent"},
         {"column":"region","op":"in","value":["APAC","Other"]}]
        """;
        var q = AggregationSqlBuilder.Build(new AggregationSpec
        {
            DatasetId = Ds, Aggregation = Aggregation.Count, FiltersJson = filters,
        }, Columns);

        Assert.Contains("= @f0", q.Sql);
        Assert.Contains("ILIKE @f1", q.Sql);
        Assert.Contains("= ANY(@f2)", q.Sql);
        Assert.Equal("Europe", q.Parameters.Single(p => p.Name == "f0").Value);
        Assert.Equal("%Ent%", q.Parameters.Single(p => p.Name == "f1").Value);
        Assert.Equal(new[] { "APAC", "Other" }, q.Parameters.Single(p => p.Name == "f2").Value);
    }

    [Fact]
    public void DateRange_AddsBoundsParams()
    {
        var q = AggregationSqlBuilder.Build(new AggregationSpec
        {
            DatasetId = Ds, Aggregation = Aggregation.Count,
            DateColumn = "event_date", From = new DateOnly(2026, 6, 1), To = new DateOnly(2026, 6, 30),
        }, Columns);

        Assert.Contains("::date >= @from", q.Sql);
        Assert.Contains("::date <= @to", q.Sql);
        Assert.Equal(new DateOnly(2026, 6, 1), q.Parameters.Single(p => p.Name == "from").Value);
    }

    [Fact]
    public void InjectionAttempt_InColumnName_Throws()
    {
        var malicious = """[{"column":"region; DROP TABLE users","op":"eq","value":"x"}]""";
        Assert.Throws<InvalidOperationException>(() => AggregationSqlBuilder.Build(
            new AggregationSpec { DatasetId = Ds, Aggregation = Aggregation.Count, FiltersJson = malicious }, Columns));
    }

    [Fact]
    public void UnknownColumn_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => AggregationSqlBuilder.Build(
            new AggregationSpec { DatasetId = Ds, Aggregation = Aggregation.Sum, MetricColumn = "ssn" }, Columns));
    }

    [Fact]
    public void NonNumericMetric_ForSum_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => AggregationSqlBuilder.Build(
            new AggregationSpec { DatasetId = Ds, Aggregation = Aggregation.Sum, MetricColumn = "region" }, Columns));
    }

    [Fact]
    public void InvalidGranularity_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => AggregationSqlBuilder.Build(
            new AggregationSpec
            {
                DatasetId = Ds, Aggregation = Aggregation.Count,
                DimensionColumn = "event_date", DateGranularity = "fortnight",
            }, Columns));
    }
}
