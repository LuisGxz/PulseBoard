using PulseBoard.Domain.Common;

namespace PulseBoard.Domain.Entities;

/// <summary>A configurable dashboard: an ordered grid of <see cref="Widget"/> bound to one dataset.</summary>
public class Dashboard : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid DatasetId { get; set; }
    public Dataset? Dataset { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Widget> Widgets { get; set; } = new List<Widget>();
    public ICollection<DashboardMember> Members { get; set; } = new List<DashboardMember>();
}
