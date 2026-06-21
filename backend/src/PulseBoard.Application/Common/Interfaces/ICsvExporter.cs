namespace PulseBoard.Application.Common.Interfaces;

/// <summary>Serializes tabular data to RFC 4180 CSV bytes (UTF-8).</summary>
public interface ICsvExporter
{
    byte[] Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows);
}
