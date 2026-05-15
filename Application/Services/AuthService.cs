using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace invoice.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    public async Task<bool> RegisterAsync(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 6)
            return false;

        var exists = await _repo.ExistsAsync(request.Phone, request.Email);
        if (exists) return false;

        await _repo.CreateAsync(request, PasswordHasher.Hash(request.Password));
        return true;
    }

    public async Task<LoginResult?> LoginAsync(UserLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password))
            return null;

        var user = await _repo.GetByPhoneAsync(request.Phone);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var expiresAt = DateTime.UtcNow.AddHours(8);
        var token = BuildToken(user, expiresAt);

        return new LoginResult(
            Token: token,
            Phone: user.Phone,
            Name: user.Name,
            ExpiresAt: expiresAt
        );
    }

    // ─── JWT ──────────────────────────────────────────────────────────────────

    private string BuildToken(UserRow user, DateTime expiresAt)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Phone),
            new Claim(ClaimTypes.Name,           user.Name),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "invoice-api",
            audience: _config["Jwt:Audience"] ?? "invoice-client",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}