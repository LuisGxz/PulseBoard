using MediatR;
using Microsoft.AspNetCore.Mvc;
using PulseBoard.Api.Infrastructure;
using PulseBoard.Application.Features.Widgets;

namespace PulseBoard.Api.Endpoints;

public static class WidgetEndpoints
{
    public record ReorderRequest(IReadOnlyList<WidgetPosition> Positions);

    public static IEndpointRouteBuilder MapWidgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboards/{dashboardId:guid}/widgets")
            .WithTags("Widgets").RequireAuthorization();

        group.MapPost("", async (
            Guid dashboardId, [FromBody] SaveWidgetRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateWidgetCommand(http.User.GetUserId(), dashboardId, body), ct);
            return Results.Created($"/api/dashboards/{dashboardId}/widgets/{result.Id}", result);
        });

        group.MapPut("/reorder", async (
            Guid dashboardId, [FromBody] ReorderRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReorderWidgetsCommand(http.User.GetUserId(), dashboardId, body.Positions), ct);
            return Results.NoContent();
        });

        group.MapPut("/{widgetId:guid}", async (
            Guid dashboardId, Guid widgetId, [FromBody] SaveWidgetRequest body,
            HttpContext http, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateWidgetCommand(http.User.GetUserId(), dashboardId, widgetId, body), ct)));

        group.MapDelete("/{widgetId:guid}", async (
            Guid dashboardId, Guid widgetId, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteWidgetCommand(http.User.GetUserId(), dashboardId, widgetId), ct);
            return Results.NoContent();
        });

        return app;
    }
}
