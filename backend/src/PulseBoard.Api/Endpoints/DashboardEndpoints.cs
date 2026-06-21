using MediatR;
using Microsoft.AspNetCore.Mvc;
using PulseBoard.Api.Infrastructure;
using PulseBoard.Application.Features.Dashboards;

namespace PulseBoard.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboards").WithTags("Dashboards").RequireAuthorization();

        group.MapGet("", async (HttpContext http, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDashboardsQuery(http.User.GetUserId()), ct)));

        group.MapGet("/{id:guid}", async (
                Guid id, DateOnly? from, DateOnly? to, HttpContext http, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDashboardDetailQuery(http.User.GetUserId(), id, from, to), ct)));

        group.MapPost("", async ([FromBody] SaveDashboardRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateDashboardCommand(http.User.GetUserId(), body.Name, body.Description, body.DatasetId), ct);
            return Results.Created($"/api/dashboards/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, [FromBody] SaveDashboardRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateDashboardCommand(http.User.GetUserId(), id, body.Name, body.Description), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteDashboardCommand(http.User.GetUserId(), id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
