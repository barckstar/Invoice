using Microsoft.AspNetCore.Mvc;
using invoice.Application.DTOs;
using invoice.Application.Services;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var ok = await _auth.RegisterAsync(request);

        if (!ok)
            return BadRequest("Invalid data or user already exists");

        return Ok("User created");
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("auth alive");
}