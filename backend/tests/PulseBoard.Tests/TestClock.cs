using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Tests;

/// <summary>Deterministic clock for time-dependent tests.</summary>
public class TestClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}
