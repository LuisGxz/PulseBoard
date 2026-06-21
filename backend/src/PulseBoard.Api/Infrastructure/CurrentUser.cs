using System.Security.Claims;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Api.Infrastructure;

/// <summary>Reads the authenticated principal off the current HTTP request.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid Id => Principal!.GetUserId();

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email") ?? "unknown";

    public AppRole Role => Enum.TryParse<AppRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role)
        ? role
        : AppRole.Member;
}
