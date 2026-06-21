using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Dashboards;

// ── Create ──────────────────────────────────────────────────────────────────────
public record CreateDashboardCommand(Guid UserId, string Name, string Description, Guid DatasetId)
    : IRequest<DashboardSummaryDto>;

public class CreateDashboardValidator : AbstractValidator<CreateDashboardCommand>
{
    public CreateDashboardValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DatasetId).NotEmpty();
    }
}

public class CreateDashboardHandler(IAppDbContext db, IClock clock)
    : IRequestHandler<CreateDashboardCommand, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(CreateDashboardCommand request, CancellationToken ct)
    {
        var dataset = await db.Datasets.FirstOrDefaultAsync(d => d.Id == request.DatasetId, ct)
            ?? throw new NotFoundException("Dataset", request.DatasetId);

        var dash = new Dashboard
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            DatasetId = dataset.Id,
            OwnerId = request.UserId,
            Slug = await UniqueSlugAsync(db, request.Name, ct),
            UpdatedAt = clock.UtcNow,
        };
        dash.Members.Add(new DashboardMember { UserId = request.UserId, Role = DashboardRole.Owner });

        db.Dashboards.Add(dash);
        await db.SaveChangesAsync(ct);

        return new DashboardSummaryDto(
            dash.Id, dash.Name, dash.Slug, dash.Description, dataset.Id, dataset.Name,
            DashboardRole.Owner.ToString(), 0, dash.UpdatedAt);
    }

    internal static async Task<string> UniqueSlugAsync(IAppDbContext db, string name, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var i = 1;
        while (await db.Dashboards.AnyAsync(d => d.Slug == slug, ct))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }

    private static string Slugify(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "dashboard" : slug;
    }
}

// ── Update (name / description) ─────────────────────────────────────────────────
public record UpdateDashboardCommand(Guid UserId, Guid Id, string Name, string Description) : IRequest;

public class UpdateDashboardValidator : AbstractValidator<UpdateDashboardCommand>
{
    public UpdateDashboardValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateDashboardHandler(IAppDbContext db, IDashboardAuthorizationService authz, IClock clock)
    : IRequestHandler<UpdateDashboardCommand>
{
    public async Task Handle(UpdateDashboardCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.Id, request.UserId, DashboardRole.Editor, ct);

        var dash = await db.Dashboards.FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            ?? throw new NotFoundException("Dashboard", request.Id);

        dash.Name = request.Name.Trim();
        dash.Description = request.Description.Trim();
        dash.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── Delete (owner only) ──────────────────────────────────────────────────────────
public record DeleteDashboardCommand(Guid UserId, Guid Id) : IRequest;

public class DeleteDashboardHandler(IAppDbContext db, IDashboardAuthorizationService authz)
    : IRequestHandler<DeleteDashboardCommand>
{
    public async Task Handle(DeleteDashboardCommand request, CancellationToken ct)
    {
        await authz.AuthorizeAsync(request.Id, request.UserId, DashboardRole.Owner, ct);

        var dash = await db.Dashboards.FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            ?? throw new NotFoundException("Dashboard", request.Id);

        db.Dashboards.Remove(dash);
        await db.SaveChangesAsync(ct);
    }
}
