using MediatR;
using Microsoft.AspNetCore.Mvc;
using PulseBoard.Api.Infrastructure;
using PulseBoard.Application.Features.Analytics;
using PulseBoard.Application.Features.Export;

namespace PulseBoard.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        // Ad-hoc aggregation for the dashboard builder's live preview.
        app.MapPost("/api/query", async (
                [FromBody] AggregationSpec spec, HttpContext http, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RunAdhocQueryCommand(http.User.GetUserId(), spec), ct)))
            .WithTags("Analytics").RequireAuthorization();

        var widgets = app.MapGroup("/api/dashboards/{dashboardId:guid}/widgets/{widgetId:guid}")
            .WithTags("Analytics").RequireAuthorization();

        widgets.MapGet("/query", async (
                Guid dashboardId, Guid widgetId, DateOnly? from, DateOnly? to,
                HttpContext http, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(
                new RunWidgetQueryCommand(http.User.GetUserId(), dashboardId, widgetId, from, to), ct)));

        widgets.MapGet("/export", async (
            Guid dashboardId, Guid widgetId, DateOnly? from, DateOnly? to,
            HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var file = await sender.Send(
                new ExportWidgetCsvCommand(http.User.GetUserId(), dashboardId, widgetId, from, to), ct);
            return Results.File(file.Content, file.ContentType, file.FileName);
        });

        return app;
    }
}
