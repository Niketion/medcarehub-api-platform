namespace MedCareHub.Api.Storage;

public interface IReportStorage
{
    Task EnsureBucketExistsAsync(CancellationToken ct);
    Task<(string bucket, string objectKey, long sizeBytes, string contentType)> UploadAsync(
        Stream data,
        string fileName,
        string contentType,
        string patientSub,
        Guid reportId,
        CancellationToken ct);

    Task<(Stream stream, string contentType, string fileName)> DownloadAsync(
        string bucket,
        string objectKey,
        string fileName,
        CancellationToken ct);
}
