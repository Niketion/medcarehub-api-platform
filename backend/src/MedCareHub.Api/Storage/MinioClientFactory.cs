using Minio;

namespace MedCareHub.Api.Storage;

public sealed class MinioClientFactory : IMinioClientFactory
{
    private readonly IConfiguration _cfg;

    public MinioClientFactory(IConfiguration cfg) => _cfg = cfg;

    public MinioClient Create()
    {
        var endpoint = _cfg["Storage:Endpoint"] ?? throw new InvalidOperationException("Storage:Endpoint missing");
        var accessKey = _cfg["Storage:AccessKey"] ?? throw new InvalidOperationException("Storage:AccessKey missing");
        var secretKey = _cfg["Storage:SecretKey"] ?? throw new InvalidOperationException("Storage:SecretKey missing");

        // endpoint can be "http://host:9000" or "host:9000"
        endpoint = endpoint.Replace("http://", "").Replace("https://", "");
        return (MinioClient)new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();
    }
}
