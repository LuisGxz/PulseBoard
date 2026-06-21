using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Infrastructure.Auth;

/// <summary>
/// Resolves and enforces per-dashboard RBAC. App-level Admins are implicitly Owners of every
/// dashboard; everyone else gets the role from their <c>DashboardMember</c> row (or no access).
/// </summary>
public class DashboardAuthorizationService(IAppDbContext db) : IDashboardAuthorizationService
{
    public async Task<DashboardRole?> GetEffectiveRoleAsync(Guid dashboardId, Guid userId, CancellationToken ct = default)
    {
        var appRole = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => (AppRole?)u.Role)
            .FirstOrDefaultAsync(ct);

        if (appRole is null)
            return null;
        if (appRole == AppRole.Admin)
            return DashboardRole.Owner;

        var membership = await db.DashboardMembers
            .Where(m => m.DashboardId == dashboardId && m.UserId == userId)
            .Select(m => (DashboardRole?)m.Role)
            .FirstOrDefaultAsync(ct);

        return membership;
    }

    public async Task AuthorizeAsync(Guid dashboardId, Guid userId, DashboardRole minRole, CancellationToken ct = default)
    {
        var role = await GetEffectiveRoleAsync(dashboardId, userId, ct);
        if (role is null || role < minRole)
            throw new ForbiddenException($"You need at least {minRole} access to this dashboard.");
    }
}
