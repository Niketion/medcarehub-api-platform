using MedCareHub.Api.Storage;

namespace MedCareHub.Api.Services;

/// <summary>
/// Ensures that MinIO storage is ready when the application starts.
/// </summary>
/// <remarks>
/// This hosted service delegates bucket initialization to the configured
/// <see cref="IReportStorage"/> implementation.
/// </remarks>
public sealed class MinioBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<MinioBootstrapHostedService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="MinioBootstrapHostedService"/>.
    /// </summary>
    public MinioBootstrapHostedService(
        IServiceProvider sp,
        ILogger<MinioBootstrapHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the report bucket exists before the API starts handling requests.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IReportStorage>();

        _logger.LogInformation("Ensuring MinIO bucket exists...");
        await storage.EnsureBucketExistsAsync(cancellationToken);
        _logger.LogInformation("MinIO bucket ready.");
    }

    /// <summary>
    /// No shutdown action is required.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}