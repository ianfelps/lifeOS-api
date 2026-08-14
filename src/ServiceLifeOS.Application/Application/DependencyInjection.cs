using Microsoft.Extensions.DependencyInjection;
using ServiceLifeOS.Application.Services;

namespace ServiceLifeOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<OperationsService>();
        services.AddScoped<FinanceService>();
        services.AddScoped<HabitService>();
        services.AddScoped<WorkoutService>();

        return services;
    }
}
