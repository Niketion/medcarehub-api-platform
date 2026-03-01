using MedCareHub.Api.Storage;

namespace MedCareHub.Api.Services;

public sealed class MinioBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<MinioBootstrapHostedService> _logger;

    public MinioBootstrapHostedService(IServiceProvider sp, ILogger<MinioBootstrapHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IReportStorage>();
        _logger.LogInformation("Ensuring MinIO bucket exists...");
        await storage.EnsureBucketExistsAsync(cancellationToken);
        _logger.LogInformation("MinIO bucket ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
