using Minio;

namespace MedCareHub.Api.Storage;

/// <summary>
/// Builds configured MinIO clients from application configuration.
/// </summary>
public sealed class MinioClientFactory : IMinioClientFactory
{
    private readonly IConfiguration _cfg;

    /// <summary>
    /// Creates a new instance of <see cref="MinioClientFactory"/>.
    /// </summary>
    /// <param name="cfg">Configuration source used to resolve MinIO settings.</param>
    public MinioClientFactory(IConfiguration cfg) => _cfg = cfg;

    /// <inheritdoc />
    public MinioClient Create()
    {
        var endpoint = _cfg["Storage:Endpoint"] ?? throw new InvalidOperationException("Storage:Endpoint missing");
        var accessKey = _cfg["Storage:AccessKey"] ?? throw new InvalidOperationException("Storage:AccessKey missing");
        var secretKey = _cfg["Storage:SecretKey"] ?? throw new InvalidOperationException("Storage:SecretKey missing");

        // The configured endpoint can be provided either as a full URL or as host:port.
        // MinIO client configuration expects only the host portion here.
        endpoint = endpoint.Replace("http://", "").Replace("https://", "");

        return (MinioClient)new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();
    }
}