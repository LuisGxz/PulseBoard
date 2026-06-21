using Microsoft.EntityFrameworkCore;
using PulseBoard.Domain.Entities;

namespace PulseBoard.Application.Common.Interfaces;

/// <summary>Abstraction over the persistence context so Application handlers stay free of EF wiring.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Dataset> Datasets { get; }
    DbSet<DatasetColumn> DatasetColumns { get; }
    DbSet<DataRow> DataRows { get; }
    DbSet<Dashboard> Dashboards { get; }
    DbSet<Widget> Widgets { get; }
    DbSet<DashboardMember> DashboardMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
