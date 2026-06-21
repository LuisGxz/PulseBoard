using MediatR;
using Microsoft.AspNetCore.Mvc;
using PulseBoard.Api.Infrastructure;
using PulseBoard.Application.Features.Auth;

namespace PulseBoard.Api.Endpoints;

public static class AuthEndpoints
{
    public record RefreshRequest(string RefreshToken);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").RequireRateLimiting("auth");

        group.MapPost("/register", async ([FromBody] RegisterCommand command, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(command, ct)));

        group.MapPost("/login", async ([FromBody] LoginCommand command, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(command, ct)));

        group.MapPost("/refresh", async ([FromBody] RefreshRequest request, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RefreshTokenCommand(request.RefreshToken), ct)));

        group.MapPost("/logout", async ([FromBody] RefreshRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new LogoutCommand(request.RefreshToken), ct);
            return Results.NoContent();
        });

        group.MapGet("/me", async (HttpContext http, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMeQuery(http.User.GetUserId()), ct)))
            .RequireAuthorization();

        return app;
    }
}
