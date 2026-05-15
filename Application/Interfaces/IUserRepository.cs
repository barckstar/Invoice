using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> ExistsAsync(string phone, string email);
    Task CreateAsync(RegisterUserRequest request, string passwordHash);
}