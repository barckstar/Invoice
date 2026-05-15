using Dapper;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Data;

namespace invoice.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly DbConnectionFactory _factory;

    public AdminRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<AdminRow?> GetByEmailAsync(string email)
    {
        using var connection = _factory.Create();
        connection.Open();

        return await connection.QueryFirstOrDefaultAsync<AdminRow>(@"
            SELECT Id, Name, Email, PasswordHash
            FROM Admins WHERE Email = @Email LIMIT 1",
            new { Email = email });
    }

    public async Task<bool> AnyAsync()
    {
        using var connection = _factory.Create();
        connection.Open();

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Admins");

        return count > 0;
    }

    public async Task CreateAsync(CreateAdminRequest request, string passwordHash)
    {
        using var connection = _factory.Create();
        connection.Open();

        await connection.ExecuteAsync(@"
            INSERT INTO Admins (Name, Email, PasswordHash, CreatedAt)
            VALUES (@Name, @Email, @PasswordHash, @CreatedAt)",
            new
            {
                request.Name,
                request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }
}