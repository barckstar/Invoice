using Dapper;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Data;

namespace invoice.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _factory;

    public UserRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> ExistsAsync(string phone, string email)
    {
        using var connection = _factory.Create();
        connection.Open();

        var count = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM Users
            WHERE Phone = @Phone OR Email = @Email",
            new { Phone = phone, Email = email });

        return count > 0;
    }

    public async Task CreateAsync(RegisterUserRequest request, string passwordHash)
    {
        using var connection = _factory.Create();
        connection.Open();

        await connection.ExecuteAsync(@"
            INSERT INTO Users (Phone, Name, Email, PasswordHash, CreatedAt)
            VALUES (@Phone, @Name, @Email, @PasswordHash, @CreatedAt)",
            new
            {
                request.Phone,
                request.Name,
                request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }

    public async Task<UserRow?> GetByPhoneAsync(string phone)
    {
        using var connection = _factory.Create();
        connection.Open();

        return await connection.QueryFirstOrDefaultAsync<UserRow>(@"
            SELECT Phone, Name, Email, PasswordHash
            FROM Users WHERE Phone = @Phone LIMIT 1",
            new { Phone = phone });
    }
}