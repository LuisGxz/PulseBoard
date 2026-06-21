using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Datasets;

/// <summary>Creates a Processing dataset, hands the CSV to the Python ETL, and returns the resulting (Ready) dataset.</summary>
public record UploadDatasetCommand(Guid UserId, string Name, string FileName, byte[] Content)
    : IRequest<DatasetSummaryDto>;

public class UploadDatasetValidator : AbstractValidator<UploadDatasetCommand>
{
    public UploadDatasetValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Content).NotEmpty().WithMessage("The uploaded file is empty.");
        RuleFor(x => x.Content).Must(c => c.Length <= 10 * 1024 * 1024).WithMessage("File exceeds the 10 MB limit.");
    }
}

public class UploadDatasetHandler(IAppDbContext db, IEtlClient etl, IClock clock)
    : IRequestHandler<UploadDatasetCommand, DatasetSummaryDto>
{
    public async Task<DatasetSummaryDto> Handle(UploadDatasetCommand request, CancellationToken ct)
    {
        var dataset = new Dataset
        {
            Name = request.Name.Trim(),
            Slug = await UniqueSlugAsync(db, request.Name, ct),
            Description = $"Uploaded from {request.FileName}",
            Source = DatasetSource.CsvUpload,
            Status = DatasetStatus.Processing,
            OwnerId = request.UserId,
            UpdatedAt = clock.UtcNow,
        };
        db.Datasets.Add(dataset);
        await db.SaveChangesAsync(ct);

        try
        {
            await etl.IngestAsync(dataset.Id, request.FileName, request.Content, ct);
        }
        catch (EtlException)
        {
            // The ETL marks Failed on a parse error; on a transport error it never ran, so mark it here.
            var fresh = await db.Datasets.FirstOrDefaultAsync(d => d.Id == dataset.Id, ct);
            if (fresh is { Status: DatasetStatus.Processing })
            {
                fresh.Status = DatasetStatus.Failed;
                fresh.UpdatedAt = clock.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            throw;
        }

        // The ETL wrote columns/rows and flipped status in the shared DB — read it back untracked.
        return await db.Datasets
            .AsNoTracking()
            .Where(d => d.Id == dataset.Id)
            .Select(d => new DatasetSummaryDto(
                d.Id, d.Name, d.Slug, d.Description, d.Status.ToString(),
                d.RowCount, d.Columns.Count, d.UpdatedAt))
            .FirstAsync(ct);
    }

    private static async Task<string> UniqueSlugAsync(IAppDbContext db, string name, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "dataset";
        var slug = baseSlug;
        var i = 1;
        while (await db.Datasets.AnyAsync(d => d.Slug == slug, ct))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }
}
