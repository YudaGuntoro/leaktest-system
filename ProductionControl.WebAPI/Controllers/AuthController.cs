using ProductionControl.Domain.Auth;
using ProductionControl.Persistence.Services.AuthService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProductionControl.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service) => _service = service;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            return ApiOk(await _service.LoginAsync(request), "Login successful");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ApiUnauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }
}
