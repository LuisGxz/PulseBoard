using PulseBoard.Domain.Common;

namespace PulseBoard.Domain.Entities;

/// <summary>Rotating refresh token. Stored hashed (SHA-256); rotation revokes the prior token.</summary>
public class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Hash of the token that superseded this one on rotation (audit trail / reuse detection).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>Revokes the token; on rotation, records the hash of its replacement.</summary>
    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
