using MedCareHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedCareHub.Api.Tests.TestHelpers;

public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly string _adminConnectionString;

    public string DatabaseName { get; }
    public string ConnectionString { get; }

    private PostgresTestDatabase(string adminConnectionString, string databaseName, string connectionString)
    {
        _adminConnectionString = adminConnectionString;
        DatabaseName = databaseName;
        ConnectionString = connectionString;
    }

    public static async Task<PostgresTestDatabase> CreateAsync(CancellationToken ct = default)
    {
        var adminConnectionString =
            Environment.GetEnvironmentVariable("PG_IT_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

        var adminBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString);
        if (string.IsNullOrWhiteSpace(adminBuilder.Database))
            adminBuilder.Database = "postgres";

        var dbName = $"medcarehub_it_{Guid.NewGuid():N}";

        await using (var conn = new NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand($@"CREATE DATABASE ""{dbName}""", conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var dbBuilder = new NpgsqlConnectionStringBuilder(adminBuilder.ConnectionString)
        {
            Database = dbName
        };

        var testDb = new PostgresTestDatabase(
            adminBuilder.ConnectionString,
            dbName,
            dbBuilder.ConnectionString);

        await using var db = testDb.CreateDbContext();
        await db.Database.MigrateAsync(ct);

        return testDb;
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var conn = new NpgsqlConnection(_adminConnectionString);
        await conn.OpenAsync();

        await using var terminate = new NpgsqlCommand(
            """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @dbName AND pid <> pg_backend_pid();
            """,
            conn);
        terminate.Parameters.AddWithValue("dbName", DatabaseName);
        await terminate.ExecuteNonQueryAsync();

        await using var drop = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{DatabaseName}""", conn);
        await drop.ExecuteNonQueryAsync();
    }
}