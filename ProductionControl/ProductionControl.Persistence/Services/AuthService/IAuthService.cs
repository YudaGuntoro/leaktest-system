using ProductionControl.Domain.Auth;

namespace ProductionControl.Persistence.Services.AuthService;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
