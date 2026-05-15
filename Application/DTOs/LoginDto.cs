namespace invoice.Application.DTOs;

public record UserLoginRequest(
    string Phone,
    string Password
);

public record LoginResult(
    string Token,
    string Phone,
    string Name,
    DateTime ExpiresAt
);