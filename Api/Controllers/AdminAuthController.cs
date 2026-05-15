using Microsoft.AspNetCore.Mvc;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _auth;

    public AdminAuthController(IAdminAuthService auth)
    {
        _auth = auth;
    }

    // POST /api/admin/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return result is null
            ? Unauthorized("Credenciales inválidas")
            : Ok(result);
    }

    // POST /api/admin/auth/seed
    // Solo funciona una vez — cuando no existe ningún admin
    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromBody] CreateAdminRequest request)
    {
        var created = await _auth.SeedAsync(request);
        return created
            ? Ok("Admin creado correctamente")
            : BadRequest("Ya existe un admin o datos inválidos (password mínimo 8 caracteres)");
    }
}