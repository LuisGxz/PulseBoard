using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Application.Features.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class RefreshTokenHandler(IAppDbContext db, IJwtTokenService jwt, AuthTokenIssuer tokenIssuer, IClock clock)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = jwt.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored?.User is null || !stored.IsActive(clock.UtcNow))
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var response = tokenIssuer.Issue(stored.User);
        stored.Revoke(clock.UtcNow, replacedByTokenHash: jwt.HashRefreshToken(response.RefreshToken));
        await db.SaveChangesAsync(ct);
        return response;
    }
}

public record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutHandler(IAppDbContext db, IJwtTokenService jwt, IClock clock) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var hash = jwt.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is not null && stored.IsActive(clock.UtcNow))
        {
            stored.Revoke(clock.UtcNow);
            await db.SaveChangesAsync(ct);
        }
        // Unknown/already-revoked tokens are a no-op: logout must be idempotent.
    }
}
