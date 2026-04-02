using MedCareHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Services;

/// <summary>
/// Applies EF Core migrations at application startup when enabled by configuration.
/// </summary>
public sealed class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="DatabaseMigrationHostedService"/>.
    /// </summary>
    public DatabaseMigrationHostedService(
        IServiceProvider sp,
        IConfiguration cfg,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _sp = sp;
        _cfg = cfg;
        _logger = logger;
    }

    /// <summary>
    /// Applies pending migrations if <c>Database:ApplyMigrationsOnStartup</c> is enabled.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var apply = _cfg.GetValue<bool>("Database:ApplyMigrationsOnStartup");
        if (!apply)
            return;

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("Migrations applied.");
    }

    /// <summary>
    /// No shutdown action is required.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}