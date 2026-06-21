namespace PulseBoard.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Secret { get; init; }
    public string Issuer { get; init; } = "PulseBoard";
    public string Audience { get; init; } = "PulseBoard";
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
