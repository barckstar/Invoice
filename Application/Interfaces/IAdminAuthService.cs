using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IAdminAuthService
{
    Task<AdminLoginResult?> LoginAsync(AdminLoginRequest request);
    Task<bool> SeedAsync(CreateAdminRequest request);
}