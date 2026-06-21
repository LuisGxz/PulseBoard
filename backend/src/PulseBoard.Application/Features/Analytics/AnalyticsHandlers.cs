using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Application.Features.Widgets;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Analytics;

// ── Ad-hoc query (dashboard builder live preview) ───────────────────────────────
public record RunAdhocQueryCommand(Guid UserId, AggregationSpec Spec) : IRequest<QueryResult>;

public class RunAdhocQueryHandler(IAnalyticsQueryService analytics)
    : IRequestHandler<RunAdhocQueryCommand, QueryResult>
{
    public Task<QueryResult> Handle(RunAdhocQueryCommand request, CancellationToken ct) =>
        analytics.RunAsync(request.Spec, ct);
}

// ── Query a single saved widget (with optional date range) ───────────────────────
public record RunWidgetQueryCommand(Guid UserId, Guid DashboardId, Guid WidgetId, DateOnly? From, DateOnly? To)
    : IRequest<QueryResult>;

public class RunWidgetQueryHandler(
    IAppDbContext db, IDashboardAuthorizationService authz, IAnalyticsQueryService analytics)
    : IRequestHandler<RunWidgetQueryCommand, QueryResult>
{
    public async Task<QueryResult> Handle(RunWidgetQueryCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.DashboardId, request.UserId, DashboardRole.Viewer, ct);

        var widget = await db.Widgets
            .Include(w => w.Dashboard)
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId && w.DashboardId == request.DashboardId, ct)
            ?? throw new NotFoundException("Widget", request.WidgetId);

        var dateColumn = await db.DatasetColumns
            .Where(c => c.DatasetId == widget.Dashboard!.DatasetId && c.Type == ColumnType.Date)
            .OrderBy(c => c.Position).Select(c => c.Name).FirstOrDefaultAsync(ct);

        var spec = WidgetSpecFactory.From(widget, widget.Dashboard!.DatasetId, dateColumn, request.From, request.To);
        return await analytics.RunAsync(spec, ct);
    }
}
