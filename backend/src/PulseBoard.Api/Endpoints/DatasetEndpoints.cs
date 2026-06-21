using MediatR;
using PulseBoard.Api.Infrastructure;
using PulseBoard.Application.Features.Datasets;

namespace PulseBoard.Api.Endpoints;

public static class DatasetEndpoints
{
    public static IEndpointRouteBuilder MapDatasetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/datasets").WithTags("Datasets").RequireAuthorization();

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDatasetsQuery(), ct)));

        group.MapPost("/upload", async (
            IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] string name,
            HttpContext http, ISender sender, CancellationToken ct) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var result = await sender.Send(
                new UploadDatasetCommand(http.User.GetUserId(), name, file.FileName, ms.ToArray()), ct);
            return Results.Created($"/api/datasets/{result.Id}", result);
        }).DisableAntiforgery();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDatasetDetailQuery(id), ct)));

        group.MapGet("/{id:guid}/rows", async (
                Guid id, int? page, int? pageSize, string? filters, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDatasetRowsQuery(id, page ?? 1, pageSize ?? 50, filters), ct)));

        return app;
    }
}
