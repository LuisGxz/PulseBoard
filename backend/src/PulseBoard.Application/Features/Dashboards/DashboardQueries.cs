using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Application.Features.Widgets;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Dashboards;

// ── List dashboards visible to the user ─────────────────────────────────────────
public record GetDashboardsQuery(Guid UserId) : IRequest<IReadOnlyList<DashboardSummaryDto>>;

public class GetDashboardsHandler(IAppDbContext db) : IRequestHandler<GetDashboardsQuery, IReadOnlyList<DashboardSummaryDto>>
{
    public async Task<IReadOnlyList<DashboardSummaryDto>> Handle(GetDashboardsQuery request, CancellationToken ct)
    {
        var isAdmin = await db.Users.AnyAsync(u => u.Id == request.UserId && u.Role == AppRole.Admin, ct);

        var query = db.Dashboards.AsQueryable();
        if (!isAdmin)
            query = query.Where(d => d.Members.Any(m => m.UserId == request.UserId));

        return await query
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DashboardSummaryDto(
                d.Id, d.Name, d.Slug, d.Description,
                d.DatasetId, d.Dataset!.Name,
                isAdmin
                    ? DashboardRole.Owner.ToString()
                    : d.Members.Where(m => m.UserId == request.UserId).Select(m => m.Role).FirstOrDefault().ToString(),
                d.Widgets.Count, d.UpdatedAt))
            .ToListAsync(ct);
    }
}

// ── Dashboard detail with every widget's computed data ──────────────────────────
public record GetDashboardDetailQuery(Guid UserId, Guid Id, DateOnly? From = null, DateOnly? To = null)
    : IRequest<DashboardDetailDto>;

public class GetDashboardDetailHandler(
    IAppDbContext db, IDashboardAuthorizationService authz, IAnalyticsQueryService analytics)
    : IRequestHandler<GetDashboardDetailQuery, DashboardDetailDto>
{
    public async Task<DashboardDetailDto> Handle(GetDashboardDetailQuery request, CancellationToken ct)
    {
        var role = await authz.GetEffectiveRoleAsync(request.Id, request.UserId, ct)
            ?? throw new ForbiddenException("You do not have access to this dashboard.");

        var dash = await db.Dashboards
            .Include(d => d.Dataset)
            .Include(d => d.Widgets)
            .Include(d => d.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            ?? throw new NotFoundException("Dashboard", request.Id);

        // A dataset's first Date column drives the optional global date-range overlay.
        var dateColumn = await db.DatasetColumns
            .Where(c => c.DatasetId == dash.DatasetId && c.Type == ColumnType.Date)
            .OrderBy(c => c.Position)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        var widgets = new List<WidgetWithDataDto>();
        foreach (var w in dash.Widgets.OrderBy(w => w.Position))
        {
            var dto = WidgetDto.From(w);
            try
            {
                var spec = WidgetSpecFactory.From(w, dash.DatasetId, dateColumn, request.From, request.To);
                var data = await analytics.RunAsync(spec, ct);
                widgets.Add(new WidgetWithDataDto(dto, data, null));
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotFoundException)
            {
                widgets.Add(new WidgetWithDataDto(dto, null, ex.Message));
            }
        }

        var members = dash.Members
            .Select(m => new DashboardMemberDto(m.UserId, m.User!.Email, m.User.DisplayName, m.Role.ToString()))
            .OrderByDescending(m => m.Role)
            .ToList();

        return new DashboardDetailDto(
            dash.Id, dash.Name, dash.Slug, dash.Description, dash.DatasetId, dash.Dataset!.Name,
            role.ToString(), widgets, members);
    }
}
