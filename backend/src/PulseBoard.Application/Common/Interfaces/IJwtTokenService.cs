using PulseBoard.Domain.Entities;

namespace PulseBoard.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <returns>The signed access token and its lifetime in seconds.</returns>
    (string AccessToken, int ExpiresInSeconds) CreateAccessToken(User user);

    /// <returns>The raw refresh token (sent to the client) and its SHA-256 hash (persisted).</returns>
    (string RawToken, string TokenHash) CreateRefreshToken();

    string HashRefreshToken(string rawToken);

    TimeSpan RefreshTokenLifetime { get; }
}
