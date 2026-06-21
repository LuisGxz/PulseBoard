namespace PulseBoard.Application.Common.Interfaces;

/// <summary>Client for the Python ETL microservice. The service parses + profiles the CSV and writes
/// columns/rows into the shared Postgres, flipping the dataset to Ready (or Failed).</summary>
public interface IEtlClient
{
    /// <summary>Ingests a CSV into an existing (Processing) dataset. Throws <see cref="Exceptions.EtlException"/> on failure.</summary>
    Task IngestAsync(Guid datasetId, string fileName, byte[] content, CancellationToken ct = default);
}
