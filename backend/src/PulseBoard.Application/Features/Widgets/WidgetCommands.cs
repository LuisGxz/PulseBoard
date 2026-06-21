using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Widgets;

internal static class WidgetMapping
{
    public static WidgetType ParseType(string value) =>
        Enum.TryParse<WidgetType>(value, ignoreCase: true, out var t)
            ? t : throw new ConflictException($"Unknown widget type '{value}'.");

    public static Aggregation ParseAggregation(string value) =>
        Enum.TryParse<Aggregation>(value, ignoreCase: true, out var a)
            ? a : throw new ConflictException($"Unknown aggregation '{value}'.");

    public static void Apply(Widget w, SaveWidgetRequest r)
    {
        w.Type = ParseType(r.Type);
        w.Title = r.Title.Trim();
        w.GridX = r.GridX; w.GridY = r.GridY; w.GridW = r.GridW; w.GridH = r.GridH;
        w.MetricColumn = Trim(r.MetricColumn);
        w.Aggregation = ParseAggregation(r.Aggregation);
        w.DimensionColumn = Trim(r.DimensionColumn);
        w.SecondaryDimensionColumn = Trim(r.SecondaryDimensionColumn);
        w.DateGranularity = Trim(r.DateGranularity);
        w.Limit = r.Limit;
        w.FiltersJson = Trim(r.FiltersJson);
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public class SaveWidgetValidator : AbstractValidator<SaveWidgetRequest>
{
    public SaveWidgetValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Aggregation).NotEmpty();
        RuleFor(x => x.GridW).GreaterThan(0);
        RuleFor(x => x.GridH).GreaterThan(0);
    }
}

// ── Create ──────────────────────────────────────────────────────────────────────
public record CreateWidgetCommand(Guid UserId, Guid DashboardId, SaveWidgetRequest Body) : IRequest<WidgetDto>;

public class CreateWidgetValidator : AbstractValidator<CreateWidgetCommand>
{
    public CreateWidgetValidator() => RuleFor(x => x.Body).SetValidator(new SaveWidgetValidator());
}

public class CreateWidgetHandler(IAppDbContext db, IDashboardAuthorizationService authz, IClock clock)
    : IRequestHandler<CreateWidgetCommand, WidgetDto>
{
    public async Task<WidgetDto> Handle(CreateWidgetCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.DashboardId, request.UserId, DashboardRole.Editor, ct);

        var dash = await db.Dashboards.FirstOrDefaultAsync(d => d.Id == request.DashboardId, ct)
            ?? throw new NotFoundException("Dashboard", request.DashboardId);

        var nextPosition = await db.Widgets.Where(w => w.DashboardId == dash.Id)
            .Select(w => (int?)w.Position).MaxAsync(ct) ?? -1;

        var widget = new Widget { DashboardId = dash.Id, Position = nextPosition + 1 };
        WidgetMapping.Apply(widget, request.Body);

        db.Widgets.Add(widget);
        dash.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        return WidgetDto.From(widget);
    }
}

// ── Update ──────────────────────────────────────────────────────────────────────
public record UpdateWidgetCommand(Guid UserId, Guid DashboardId, Guid WidgetId, SaveWidgetRequest Body) : IRequest<WidgetDto>;

public class UpdateWidgetValidator : AbstractValidator<UpdateWidgetCommand>
{
    public UpdateWidgetValidator() => RuleFor(x => x.Body).SetValidator(new SaveWidgetValidator());
}

public class UpdateWidgetHandler(IAppDbContext db, IDashboardAuthorizationService authz, IClock clock)
    : IRequestHandler<UpdateWidgetCommand, WidgetDto>
{
    public async Task<WidgetDto> Handle(UpdateWidgetCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.DashboardId, request.UserId, DashboardRole.Editor, ct);

        var widget = await db.Widgets
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId && w.DashboardId == request.DashboardId, ct)
            ?? throw new NotFoundException("Widget", request.WidgetId);

        WidgetMapping.Apply(widget, request.Body);
        await TouchDashboard(db, request.DashboardId, clock, ct);
        await db.SaveChangesAsync(ct);

        return WidgetDto.From(widget);
    }

    internal static async Task TouchDashboard(IAppDbContext db, Guid dashboardId, IClock clock, CancellationToken ct)
    {
        var dash = await db.Dashboards.FirstOrDefaultAsync(d => d.Id == dashboardId, ct);
        if (dash is not null) dash.UpdatedAt = clock.UtcNow;
    }
}

// ── Delete ──────────────────────────────────────────────────────────────────────
public record DeleteWidgetCommand(Guid UserId, Guid DashboardId, Guid WidgetId) : IRequest;

public class DeleteWidgetHandler(IAppDbContext db, IDashboardAuthorizationService authz, IClock clock)
    : IRequestHandler<DeleteWidgetCommand>
{
    public async Task Handle(DeleteWidgetCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.DashboardId, request.UserId, DashboardRole.Editor, ct);

        var widget = await db.Widgets
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId && w.DashboardId == request.DashboardId, ct)
            ?? throw new NotFoundException("Widget", request.WidgetId);

        db.Widgets.Remove(widget);
        await UpdateWidgetHandler.TouchDashboard(db, request.DashboardId, clock, ct);
        await db.SaveChangesAsync(ct);
    }
}

// ── Reorder / reposition (drag & drop persistence) ───────────────────────────────
public record WidgetPosition(Guid WidgetId, int Position, int GridX, int GridY, int GridW, int GridH);
public record ReorderWidgetsCommand(Guid UserId, Guid DashboardId, IReadOnlyList<WidgetPosition> Positions) : IRequest;

public class ReorderWidgetsHandler(IAppDbContext db, IDashboardAuthorizationService authz, IClock clock)
    : IRequestHandler<ReorderWidgetsCommand>
{
    public async Task Handle(ReorderWidgetsCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.DashboardId, request.UserId, DashboardRole.Editor, ct);

        var widgets = await db.Widgets.Where(w => w.DashboardId == request.DashboardId).ToListAsync(ct);
        var byId = widgets.ToDictionary(w => w.Id);

        foreach (var pos in request.Positions)
        {
            if (!byId.TryGetValue(pos.WidgetId, out var w)) continue;
            w.Position = pos.Position;
            w.GridX = pos.GridX; w.GridY = pos.GridY; w.GridW = pos.GridW; w.GridH = pos.GridH;
        }

        await UpdateWidgetHandler.TouchDashboard(db, request.DashboardId, clock, ct);
        await db.SaveChangesAsync(ct);
    }
}
