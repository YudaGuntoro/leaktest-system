using ProductionControl.Persistence.Services.AuthService;
using Microsoft.Extensions.DependencyInjection;

namespace ProductionControl.Persistence.IoC;

public static class DependencyContainer
{
    public static void AddIoCService(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
    }
}
