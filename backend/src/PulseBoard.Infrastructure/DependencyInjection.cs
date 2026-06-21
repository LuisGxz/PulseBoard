using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PulseBoard.Application.Common.Interfaces;
using PulseBoard.Infrastructure.Analytics;
using PulseBoard.Infrastructure.Auth;
using PulseBoard.Infrastructure.Common;
using PulseBoard.Infrastructure.Data;
using PulseBoard.Infrastructure.Etl;

namespace PulseBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
                 ?? "Host=localhost;Port=55432;Database=pulseboard;Username=pulseboard;Password=PulseBoard_Dev!2026";

        services.AddDbContext<PulseBoardDbContext>(opt => opt.UseNpgsql(cs));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<PulseBoardDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret) && o.Secret.Length >= 32,
                "Jwt:Secret must be configured with at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IDashboardAuthorizationService, DashboardAuthorizationService>();
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddSingleton<ICsvExporter, CsvExporter>();

        services.AddOptions<EtlOptions>().Bind(config.GetSection(EtlOptions.SectionName));
        services.AddHttpClient<IEtlClient, EtlClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<EtlOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("X-ETL-Key", opt.ApiKey);
        });

        return services;
    }
}
