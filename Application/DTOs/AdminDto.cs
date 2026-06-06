namespace invoice.Application.DTOs;

public record AdminLoginRequest(
    string Email,
    string Password
);

public record AdminLoginResult(
    string Token,
    string Name,
    string Email,
    DateTime ExpiresAt
);

public record CreateAdminRequest(
    string Name,
    string Email,
    string Password
);

public class AdminRow
{
    public long Id { get; init; } 
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string PasswordHash { get; init; } = "";
}