using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Infrastructure.Common;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
