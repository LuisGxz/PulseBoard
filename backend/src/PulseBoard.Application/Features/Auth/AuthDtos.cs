using PulseBoard.Domain.Entities;

namespace PulseBoard.Application.Features.Auth;

public record UserDto(Guid Id, string Email, string DisplayName, string Role)
{
    public static UserDto From(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role.ToString());
}

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, UserDto User);
