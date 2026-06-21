using PulseBoard.Domain.Common;
using PulseBoard.Domain.Enums;

namespace PulseBoard.Domain.Entities;

/// <summary>Join entity carrying a user's RBAC role on a specific dashboard.</summary>
public class DashboardMember : Entity
{
    public Guid DashboardId { get; set; }
    public Dashboard? Dashboard { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DashboardRole Role { get; set; } = DashboardRole.Viewer;
}
