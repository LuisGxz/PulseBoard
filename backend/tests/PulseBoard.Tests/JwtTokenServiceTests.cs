using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;
using PulseBoard.Infrastructure.Auth;

namespace PulseBoard.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService Service() => new(Options.Create(new JwtOptions
    {
        Secret = "test-signing-key-at-least-32-chars-long-000",
        Issuer = "PulseBoard",
        Audience = "PulseBoard",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    }));

    [Fact]
    public void AccessToken_CarriesIdentityClaims()
    {
        var user = new User { Email = "a@b.io", DisplayName = "Ada", Role = AppRole.Admin };
        var (token, expiresIn) = Service().CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal("a@b.io", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(nameof(AppRole.Admin), jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(15 * 60, expiresIn);
    }

    [Fact]
    public void RefreshToken_HashIsDeterministic_AndMatchesRaw()
    {
        var svc = Service();
        var (raw, hash) = svc.CreateRefreshToken();

        Assert.Equal(hash, svc.HashRefreshToken(raw));
        Assert.NotEqual(raw, hash); // the raw token is never stored verbatim
    }

    [Fact]
    public void RefreshToken_Lifetime_MatchesConfiguredDays()
    {
        Assert.Equal(TimeSpan.FromDays(7), Service().RefreshTokenLifetime);
    }
}
