using PulseBoard.Domain.Common;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Domain.Entities;

/// <summary>
/// One column of a dataset with its inferred type and profiling stats (computed by the ETL service).
/// Drives which aggregations/dimensions are offered in the dashboard builder.
/// </summary>
public class DatasetColumn : Entity
{
    public Guid DatasetId { get; set; }
    public Dataset? Dataset { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ColumnType Type { get; set; } = ColumnType.String;
    public int Position { get; set; }

    // Profile (nullable — populated at ingest)
    public long NullCount { get; set; }
    public long DistinctCount { get; set; }
    public double? MinNumeric { get; set; }
    public double? MaxNumeric { get; set; }
    public string? SampleValues { get; set; } // JSON array of a few example values
}
