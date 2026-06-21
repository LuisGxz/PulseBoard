using PulseBoard.Domain.Common;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Domain.Entities;

public class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AppRole Role { get; set; } = AppRole.Member;

    // Lockout (brute-force protection).
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedOutUntil { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<DashboardMember> Memberships { get; set; } = new List<DashboardMember>();

    /// <summary>After this many consecutive failures the account locks for <see cref="LockoutDuration"/>.</summary>
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public bool IsLockedOut(DateTimeOffset now) => LockedOutUntil is { } until && until > now;

    /// <summary>Records a failed attempt; locks the account once the threshold is reached.</summary>
    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLogins)
        {
            LockedOutUntil = now.Add(LockoutDuration);
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedOutUntil = null;
    }
}
