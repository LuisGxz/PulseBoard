using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Domain.Entities;

namespace PulseBoard.Infrastructure.Data;

public class PulseBoardDbContext(DbContextOptions<PulseBoardDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetColumn> DatasetColumns => Set<DatasetColumn>();
    public DbSet<DataRow> DataRows => Set<DataRow>();
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<Widget> Widgets => Set<Widget>();
    public DbSet<DashboardMember> DashboardMembers => Set<DashboardMember>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.TokenHash);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Dataset>(e =>
        {
            e.ToTable("datasets");
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Columns).WithOne(c => c.Dataset!).HasForeignKey(c => c.DatasetId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DatasetColumn>(e =>
        {
            e.ToTable("dataset_columns");
            e.HasIndex(x => new { x.DatasetId, x.Name }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Label).HasMaxLength(160);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SampleValues).HasColumnType("jsonb");
        });

        b.Entity<DataRow>(e =>
        {
            e.ToTable("dataset_rows");
            e.HasIndex(x => x.DatasetId);
            e.Property(x => x.Data).HasColumnType("jsonb").IsRequired();
            e.HasOne(x => x.Dataset).WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Dashboard>(e =>
        {
            e.ToTable("dashboards");
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasOne(x => x.Dataset).WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Widgets).WithOne(w => w.Dashboard!).HasForeignKey(w => w.DashboardId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Members).WithOne(m => m.Dashboard!).HasForeignKey(m => m.DashboardId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Widget>(e =>
        {
            e.ToTable("widgets");
            e.HasIndex(x => x.DashboardId);
            e.Property(x => x.Title).HasMaxLength(160);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Aggregation).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MetricColumn).HasMaxLength(120);
            e.Property(x => x.DimensionColumn).HasMaxLength(120);
            e.Property(x => x.SecondaryDimensionColumn).HasMaxLength(120);
            e.Property(x => x.DateGranularity).HasMaxLength(20);
            e.Property(x => x.FiltersJson).HasColumnType("jsonb");
        });

        b.Entity<DashboardMember>(e =>
        {
            e.ToTable("dashboard_members");
            e.HasIndex(x => new { x.DashboardId, x.UserId }).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.User).WithMany(u => u.Memberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
