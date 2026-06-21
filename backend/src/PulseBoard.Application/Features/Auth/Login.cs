using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler(IAppDbContext db, IPasswordHasher hasher, AuthTokenIssuer tokenIssuer, IClock clock)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);

        // Same error for unknown email and wrong password: do not leak which accounts exist.
        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        if (user.IsLockedOut(clock.UtcNow))
            throw new UnauthorizedException("Account temporarily locked. Try again later.");

        if (!hasher.Verify(user.PasswordHash, request.Password))
        {
            user.RegisterFailedLogin(clock.UtcNow);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.RegisterSuccessfulLogin();
        var response = tokenIssuer.Issue(user);
        await db.SaveChangesAsync(ct);
        return response;
    }
}
