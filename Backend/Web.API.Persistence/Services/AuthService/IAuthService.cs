using Web.API.Domain.Auth;

namespace Web.API.Persistence.Services.AuthService;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
