using System.Security.Claims;

namespace PulseBoard.Api.Infrastructure;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated principal has no valid user id claim.");
    }
}
