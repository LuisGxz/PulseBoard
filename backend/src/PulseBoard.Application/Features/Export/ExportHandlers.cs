using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Application.Features.Analytics;
using PulseBoard.Application.Features.Widgets;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Export;

public record FileResult(byte[] Content, string FileName, string ContentType);

// ── Export a widget's aggregated result as CSV (report export) ───────────────────
public record ExportWidgetCsvCommand(Guid UserId, Guid DashboardId, Guid WidgetId, DateOnly? From, DateOnly? To)
    : IRequest<FileResult>;

public class ExportWidgetCsvHandler(
    IAppDbContext db, IDashboardAuthorizationService authz, IAnalyticsQueryService analytics, ICsvExporter csv)
    : IRequestHandler<ExportWidgetCsvCommand, FileResult>
{
    public async Task<FileResult> Handle(ExportWidgetCsvCommand request, CancellationToken ct)
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
        var result = await analytics.RunAsync(spec, ct);

        var (headers, rows) = Flatten(widget.DimensionColumn, widget.SecondaryDimensionColumn, result);
        var bytes = csv.Write(headers, rows);
        var fileName = $"{Slug(widget.Title)}.csv";
        return new FileResult(bytes, fileName, "text/csv");
    }

    private static (IReadOnlyList<string> Headers, IEnumerable<IReadOnlyList<object?>> Rows) Flatten(
        string? dim, string? secondaryDim, QueryResult result)
    {
        switch (result.Kind)
        {
            case "matrix":
                return (
                    [dim ?? "x", secondaryDim ?? "y", "value"],
                    (result.Matrix ?? []).Select(c => (IReadOnlyList<object?>)[c.X, c.Y, c.Value]));
            case "series":
                return (
                    [dim ?? "key", "value"],
                    (result.Points ?? []).Select(p => (IReadOnlyList<object?>)[p.Key, p.Value]));
            default:
                return (["value"], [[result.Scalar]]);
        }
    }

    private static string Slug(string title)
    {
        var slug = new string(title.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        return string.IsNullOrEmpty(slug) ? "report" : slug;
    }
}
