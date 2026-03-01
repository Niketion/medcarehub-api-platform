using Minio;

namespace MedCareHub.Api.Storage;

public interface IMinioClientFactory
{
    MinioClient Create();
}
