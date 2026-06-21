using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Infrastructure.Etl;

/// <summary>Typed HttpClient over the Python ETL service. The shared X-ETL-Key is sent on every call.</summary>
public class EtlClient(HttpClient http) : IEtlClient
{
    public async Task IngestAsync(Guid datasetId, string fileName, byte[] content, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(datasetId.ToString()), "dataset_id" },
        };
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", string.IsNullOrWhiteSpace(fileName) ? "upload.csv" : fileName);

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsync("/ingest", form, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new EtlException("The ETL service is unreachable. Please try again shortly.");
        }

        if (!response.IsSuccessStatusCode)
            throw new EtlException(await ReadDetailAsync(response, ct));
    }

    /// <summary>Pulls the FastAPI error detail ({"detail": "..."}) so the user sees why the CSV was rejected.</summary>
    private static async Task<string> ReadDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (problem.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString()!;
        }
        catch (JsonException) { /* fall through */ }
        return $"The ETL service rejected the file ({(int)response.StatusCode}).";
    }
}
