using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Common.Interfaces;

/// <summary>
/// Per-dashboard RBAC. Resolves a user's effective <see cref="DashboardRole"/> on a dashboard and
/// enforces a minimum role. App-level <see cref="AppRole.Admin"/> is treated as Owner everywhere.
/// </summary>
public interface IDashboardAuthorizationService
{
    /// <returns>The user's effective role on the dashboard, or null if they have no access.</returns>
    Task<DashboardRole?> GetEffectiveRoleAsync(Guid dashboardId, Guid userId, CancellationToken ct = default);

    /// <summary>Throws <see cref="Exceptions.ForbiddenException"/> if the user lacks <paramref name="minRole"/>.</summary>
    Task AuthorizeAsync(Guid dashboardId, Guid userId, DashboardRole minRole, CancellationToken ct = default);
}
