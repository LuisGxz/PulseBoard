using PulseBoard.Domain.Enums;

namespace PulseBoard.Application.Common.Interfaces;

/// <summary>Ambient info about the authenticated principal making the current request.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid Id { get; }
    string Email { get; }
    AppRole Role { get; }
}
