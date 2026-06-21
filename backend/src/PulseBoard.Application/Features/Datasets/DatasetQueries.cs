using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Application.Features.Datasets;

// ── List ──────────────────────────────────────────────────────────────────────
public record GetDatasetsQuery : IRequest<IReadOnlyList<DatasetSummaryDto>>;

public class GetDatasetsHandler(IAppDbContext db) : IRequestHandler<GetDatasetsQuery, IReadOnlyList<DatasetSummaryDto>>
{
    public async Task<IReadOnlyList<DatasetSummaryDto>> Handle(GetDatasetsQuery request, CancellationToken ct) =>
        await db.Datasets
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DatasetSummaryDto(
                d.Id, d.Name, d.Slug, d.Description, d.Status.ToString(),
                d.RowCount, d.Columns.Count, d.UpdatedAt))
            .ToListAsync(ct);
}

// ── Detail ────────────────────────────────────────────────────────────────────
public record GetDatasetDetailQuery(Guid Id) : IRequest<DatasetDetailDto>;

public class GetDatasetDetailHandler(IAppDbContext db) : IRequestHandler<GetDatasetDetailQuery, DatasetDetailDto>
{
    public async Task<DatasetDetailDto> Handle(GetDatasetDetailQuery request, CancellationToken ct)
    {
        var ds = await db.Datasets
            .Include(d => d.Columns)
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            ?? throw new NotFoundException("Dataset", request.Id);

        return new DatasetDetailDto(
            ds.Id, ds.Name, ds.Slug, ds.Description, ds.Status.ToString(), ds.RowCount,
            ds.Columns.OrderBy(c => c.Position).Select(DatasetColumnDto.From).ToList());
    }
}

// ── Paginated rows (dataset table view) ─────────────────────────────────────────
public record GetDatasetRowsQuery(Guid Id, int Page, int PageSize, string? FiltersJson)
    : IRequest<DatasetRowsDto>;

public class GetDatasetRowsHandler(IAppDbContext db, IAnalyticsQueryService analytics)
    : IRequestHandler<GetDatasetRowsQuery, DatasetRowsDto>
{
    public async Task<DatasetRowsDto> Handle(GetDatasetRowsQuery request, CancellationToken ct)
    {
        var columns = await db.DatasetColumns
            .Where(c => c.DatasetId == request.Id)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        if (columns.Count == 0)
            throw new NotFoundException("Dataset", request.Id);

        var (rows, total) = await analytics.GetRowsAsync(request.Id, request.Page, request.PageSize, request.FiltersJson, ct);

        return new DatasetRowsDto(
            total, Math.Max(request.Page, 1), Math.Clamp(request.PageSize, 1, 200),
            columns.Select(DatasetColumnDto.From).ToList(), rows);
    }
}
