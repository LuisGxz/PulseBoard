using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseBoard.Infrastructure.Data;

/// <summary>Used by <c>dotnet ef</c> at design time. Runtime wiring lives in DependencyInjection.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PulseBoardDbContext>
{
    public PulseBoardDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Port=55432;Database=pulseboard;Username=pulseboard;Password=PulseBoard_Dev!2026";
        var options = new DbContextOptionsBuilder<PulseBoardDbContext>()
            .UseNpgsql(cs)
            .Options;
        return new PulseBoardDbContext(options);
    }
}
