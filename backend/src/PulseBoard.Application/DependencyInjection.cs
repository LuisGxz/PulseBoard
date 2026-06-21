using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PulseBoard.Application.Common.Behaviors;
using PulseBoard.Application.Features.Auth;

namespace PulseBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<AuthTokenIssuer>();
        return services;
    }
}
