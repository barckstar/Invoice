using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> ExistsAsync(string phone, string email);
    Task CreateAsync(RegisterUserRequest request, string passwordHash);
    Task<UserRow?> GetByPhoneAsync(string phone);
}

public record UserRow(
    string Phone,
    string Name,
    string Email,
    string PasswordHash
);