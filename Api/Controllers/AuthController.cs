using Microsoft.AspNetCore.Mvc;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var ok = await _auth.RegisterAsync(request);
        return ok
            ? Ok("User created")
            : BadRequest("Invalid data or user already exists");
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return result is null
            ? Unauthorized("Invalid credentials")
            : Ok(result);
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("auth alive");
}