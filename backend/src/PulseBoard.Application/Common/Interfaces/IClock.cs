namespace PulseBoard.Application.Common.Interfaces;

/// <summary>Abstracts the system clock so time-dependent logic (lockout, token expiry) is testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
