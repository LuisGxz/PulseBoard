namespace PulseBoard.Infrastructure.Etl;

public class EtlOptions
{
    public const string SectionName = "Etl";

    public string BaseUrl { get; init; } = "http://localhost:8100";
    public string ApiKey { get; init; } = "pulseboard-etl-dev-key";
}
