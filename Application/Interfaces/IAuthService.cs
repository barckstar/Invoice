using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterUserRequest request);
    Task<LoginResult?> LoginAsync(UserLoginRequest request);
}