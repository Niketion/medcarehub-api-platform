using Minio;

namespace MedCareHub.Api.Storage;

/// <summary>
/// Provides configured <see cref="MinioClient"/> instances for application services.
/// </summary>
/// <remarks>
/// The factory centralizes endpoint and credential resolution from configuration
/// so that storage components do not duplicate connection setup logic.
/// </remarks>
public interface IMinioClientFactory
{
    /// <summary>
    /// Creates a configured MinIO client instance.
    /// </summary>
    /// <returns>A ready-to-use <see cref="MinioClient"/>.</returns>
    MinioClient Create();
}