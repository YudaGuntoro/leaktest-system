using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;
using Web.API.Domain.Response;

namespace Web.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ApiOk<T>(T data, string message = "Data retrieved successfully")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            StatusCode = (int)HttpStatusCode.OK,
            Message = message,
            Data = data
        });
    }

    protected IActionResult ApiCreated<T>(T data, string message = "Data created successfully")
    {
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<T>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = message,
            Data = data
        });
    }

    protected IActionResult ApiNotFound(string message = "Data not found")
    {
        return NotFound(new ApiResponse<string>
        {
            Success = false,
            StatusCode = (int)HttpStatusCode.NotFound,
            Message = message,
            Data = null
        });
    }

    protected IActionResult ApiBadRequest(Exception ex)
    {
        Log.Error(ex, "Request failed: {Method} {Path}", HttpContext.Request.Method, HttpContext.Request.Path);

        var baseException = ex.GetBaseException();
        var message = baseException.Message != ex.Message
            ? $"{ex.Message} Detail: {baseException.Message}"
            : ex.Message;

        return BadRequest(new ApiResponse<string>
        {
            Success = false,
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = message,
            Data = null
        });
    }

    protected IActionResult ApiUnauthorized(string message)
    {
        return Unauthorized(new ApiResponse<string>
        {
            Success = false,
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Message = message,
            Data = null
        });
    }
}
