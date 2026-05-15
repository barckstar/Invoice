using invoice.Application.DTOs;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Security;

namespace invoice.Application.Services;

public class AuthService
{
    private readonly IUserRepository _repo;

    public AuthService(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> RegisterAsync(RegisterUserRequest request)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 6)
            return false;

        var exists = await _repo.ExistsAsync(request.Phone, request.Email);
        if (exists) return false;

        var hash = PasswordHasher.Hash(request.Password);

        await _repo.CreateAsync(request, hash);

        return true;
    }
}