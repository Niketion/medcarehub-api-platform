using MedCareHub.Api.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio.DataModel.Args;

namespace MedCareHub.Api.Health;

public sealed class MinioHealthCheck : IHealthCheck
{
    private readonly IMinioClientFactory _factory;
    private readonly IConfiguration _cfg;

    public MinioHealthCheck(IMinioClientFactory factory, IConfiguration cfg)
    {
        _factory = factory;
        _cfg = cfg;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _factory.Create();
            var bucket = _cfg["Storage:Bucket"] ?? "reports";

            var exists = await client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucket),
                cancellationToken);

            return exists
                ? HealthCheckResult.Healthy($"MinIO reachable, bucket '{bucket}' available.")
                : HealthCheckResult.Degraded($"MinIO reachable, bucket '{bucket}' not found.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO unreachable.", ex);
        }
    }
}