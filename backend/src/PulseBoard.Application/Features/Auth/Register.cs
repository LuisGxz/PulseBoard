using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Features.Auth;

/// <summary>Public self-registration always creates a Member. Admin accounts are seeded/provisioned.</summary>
public record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<AuthResponse>;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public class RegisterHandler(IAppDbContext db, IPasswordHasher hasher, AuthTokenIssuer tokenIssuer)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Role = AppRole.Member,
            PasswordHash = hasher.Hash(request.Password),
        };
        db.Users.Add(user);

        var response = tokenIssuer.Issue(user);
        await db.SaveChangesAsync(ct);
        return response;
    }
}
