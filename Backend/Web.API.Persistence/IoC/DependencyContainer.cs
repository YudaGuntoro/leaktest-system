using Microsoft.Extensions.DependencyInjection;
using Web.API.Persistence.Services.AuthService;

namespace Web.API.Persistence.IoC;

public static class DependencyContainer
{
    public static void AddIoCService(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
    }
}
