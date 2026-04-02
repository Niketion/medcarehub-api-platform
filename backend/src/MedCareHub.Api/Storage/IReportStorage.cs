namespace MedCareHub.Api.Storage;

/// <summary>
/// Defines the storage contract for clinical report files.
/// </summary>
/// <remarks>
/// Implementations are responsible for bucket bootstrap, upload persistence
/// and download retrieval while keeping the API layer storage-agnostic.
/// </remarks>
public interface IReportStorage
{
    /// <summary>
    /// Ensures that the target storage bucket exists.
    /// </summary>
    /// <param name="ct">Cancellation token for the current operation.</param>
    /// <returns>A task that completes when the bucket is ready.</returns>
    Task EnsureBucketExistsAsync(CancellationToken ct);

    /// <summary>
    /// Uploads a report file and returns the persisted storage coordinates.
    /// </summary>
    /// <param name="data">File content stream.</param>
    /// <param name="fileName">Original uploaded file name.</param>
    /// <param name="contentType">Declared MIME type.</param>
    /// <param name="patientSub">Patient identifier used to build the object path.</param>
    /// <param name="reportId">Report identifier used to build the object path.</param>
    /// <param name="ct">Cancellation token for the current operation.</param>
    /// <returns>
    /// The storage bucket, object key, stored size and resolved content type.
    /// </returns>
    Task<(string bucket, string objectKey, long sizeBytes, string contentType)> UploadAsync(
        Stream data,
        string fileName,
        string contentType,
        string patientSub,
        Guid reportId,
        CancellationToken ct);

    /// <summary>
    /// Downloads a report file from storage.
    /// </summary>
    /// <param name="bucket">Bucket containing the file.</param>
    /// <param name="objectKey">Storage object key.</param>
    /// <param name="fileName">Logical file name exposed to the client.</param>
    /// <param name="ct">Cancellation token for the current operation.</param>
    /// <returns>The file stream together with content type and file name.</returns>
    Task<(Stream stream, string contentType, string fileName)> DownloadAsync(
        string bucket,
        string objectKey,
        string fileName,
        CancellationToken ct);
}