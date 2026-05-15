using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace invoice.Application.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly IAdminRepository _repo;
    private readonly IConfiguration _config;

    public AdminAuthService(IAdminRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    public async Task<AdminLoginResult?> LoginAsync(AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return null;

        var admin = await _repo.GetByEmailAsync(request.Email);

        if (admin is null || !PasswordHasher.Verify(request.Password, admin.PasswordHash))
            return null;

        var expiresAt = DateTime.UtcNow.AddHours(8);

        return new AdminLoginResult(
            Token: BuildToken(admin, expiresAt),
            Name: admin.Name,
            Email: admin.Email,
            ExpiresAt: expiresAt
        );
    }

    // Solo funciona si no existe NINGUN admin en la DB
    public async Task<bool> SeedAsync(CreateAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
            return false;

        var alreadyExists = await _repo.AnyAsync();
        if (alreadyExists) return false;

        await _repo.CreateAsync(request, PasswordHasher.Hash(request.Password));
        return true;
    }

    private string BuildToken(AdminRow admin, DateTime expiresAt)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name,           admin.Name),
            new Claim(ClaimTypes.Email,          admin.Email),
            new Claim(ClaimTypes.Role,           "admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "invoice-api",
            audience: _config["Jwt:Audience"] ?? "invoice-client",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}