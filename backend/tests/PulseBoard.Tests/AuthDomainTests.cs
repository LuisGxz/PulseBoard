using PulseBoard.Domain.Entities;

namespace PulseBoard.Tests;

public class UserLockoutTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewUser_IsNotLockedOut()
    {
        var user = new User();
        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void FailedLogins_BelowThreshold_DoNotLock()
    {
        var user = new User();
        for (var i = 0; i < User.MaxFailedLogins - 1; i++)
            user.RegisterFailedLogin(Now);

        Assert.False(user.IsLockedOut(Now));
        Assert.Equal(User.MaxFailedLogins - 1, user.FailedLoginCount);
    }

    [Fact]
    public void ReachingThreshold_LocksForConfiguredDuration_AndResetsCounter()
    {
        var user = new User();
        for (var i = 0; i < User.MaxFailedLogins; i++)
            user.RegisterFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now));
        Assert.Equal(0, user.FailedLoginCount); // counter resets when the lock is applied
        Assert.False(user.IsLockedOut(Now.Add(User.LockoutDuration).AddSeconds(1))); // lock expires
    }

    [Fact]
    public void SuccessfulLogin_ClearsFailuresAndLock()
    {
        var user = new User();
        user.RegisterFailedLogin(Now);
        user.RegisterFailedLogin(Now);

        user.RegisterSuccessfulLogin();

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedOutUntil);
        Assert.False(user.IsLockedOut(Now));
    }
}

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Token_IsActive_WhenNotRevokedAndNotExpired()
    {
        var token = new RefreshToken { TokenHash = "h", ExpiresAt = Now.AddDays(1) };
        Assert.True(token.IsActive(Now));
    }

    [Fact]
    public void Token_IsInactive_WhenExpired()
    {
        var token = new RefreshToken { TokenHash = "h", ExpiresAt = Now.AddDays(-1) };
        Assert.False(token.IsActive(Now));
    }

    [Fact]
    public void Revoke_MarksInactive_AndRecordsReplacement()
    {
        var token = new RefreshToken { TokenHash = "old", ExpiresAt = Now.AddDays(1) };
        token.Revoke(Now, replacedByTokenHash: "new");

        Assert.False(token.IsActive(Now));
        Assert.Equal(Now, token.RevokedAt);
        Assert.Equal("new", token.ReplacedByTokenHash);
    }
}
