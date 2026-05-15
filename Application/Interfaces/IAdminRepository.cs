using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IAdminRepository
{
    Task<AdminRow?> GetByEmailAsync(string email);
    Task<bool> AnyAsync();
    Task CreateAsync(CreateAdminRequest request, string passwordHash);
}

public record AdminRow(
    int Id,
    string Name,
    string Email,
    string PasswordHash
);