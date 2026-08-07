using Microsoft.Extensions.DependencyInjection;
using ServiceLifeOS.Application.Services;

namespace ServiceLifeOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();

        return services;
    }
}
