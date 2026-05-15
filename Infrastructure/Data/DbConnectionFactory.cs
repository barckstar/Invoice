using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace invoice.Infrastructure.Data;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Sqlite")
            ?? throw new ArgumentNullException("Connection string not found");
    }

    public IDbConnection Create()
    {
        return new SqliteConnection(_connectionString);
    }
}