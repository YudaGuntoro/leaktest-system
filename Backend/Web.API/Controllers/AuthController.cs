using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.API.Domain.Auth;
using Web.API.Persistence.Services.AuthService;

namespace Web.API.Controllers;

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
