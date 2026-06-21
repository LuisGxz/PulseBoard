using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Entities;

namespace PulseBoard.Application.Features.Auth;

/// <summary>Issues an access token + persisted rotating refresh token for a user.</summary>
public class AuthTokenIssuer(IAppDbContext db, IJwtTokenService jwt, IClock clock)
{
    public AuthResponse Issue(User user)
    {
        var (accessToken, expiresIn) = jwt.CreateAccessToken(user);
        var (rawRefresh, refreshHash) = jwt.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = clock.UtcNow.Add(jwt.RefreshTokenLifetime),
        });

        return new AuthResponse(accessToken, rawRefresh, expiresIn, UserDto.From(user));
    }
}
