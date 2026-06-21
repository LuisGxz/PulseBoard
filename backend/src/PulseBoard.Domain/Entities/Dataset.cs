using PulseBoard.Domain.Common;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Domain.Entities;

/// <summary>
/// A tabular dataset ingested from CSV (or a bundled sample). Rows live in <see cref="DataRow"/>
/// as JSONB; the schema/profile lives in <see cref="DatasetColumn"/>. The Python ETL service owns
/// ingestion and profiling; the .NET API owns metadata and orchestration.
/// </summary>
public class Dataset : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DatasetSource Source { get; set; } = DatasetSource.CsvUpload;
    public DatasetStatus Status { get; set; } = DatasetStatus.Processing;
    public string? StatusMessage { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public long RowCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<DatasetColumn> Columns { get; set; } = new List<DatasetColumn>();
}
