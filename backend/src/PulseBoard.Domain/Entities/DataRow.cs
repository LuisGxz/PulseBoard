using PulseBoard.Domain.Common;

namespace PulseBoard.Domain.Entities;

/// <summary>
/// A single record of a dataset, stored schema-agnostically as JSONB keyed by column name.
/// Aggregation queries run over <c>dataset_rows.data</c> via Postgres JSONB expressions (and pandas in the ETL service).
/// </summary>
public class DataRow : Entity
{
    public Guid DatasetId { get; set; }
    public Dataset? Dataset { get; set; }

    /// <summary>Raw JSON object (column name → value). Mapped to a Postgres <c>jsonb</c> column.</summary>
    public string Data { get; set; } = "{}";
}
