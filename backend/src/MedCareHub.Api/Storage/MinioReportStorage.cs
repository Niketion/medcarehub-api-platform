using Minio;
using Minio.DataModel.Args;

namespace MedCareHub.Api.Storage;

/// <summary>
/// Stores clinical reports in MinIO object storage.
/// </summary>
/// <remarks>
/// Object keys are organized by patient identifier and report identifier
/// so that files remain grouped and easy to trace.
/// </remarks>
public sealed class MinioReportStorage : IReportStorage
{
    private readonly IMinioClientFactory _factory;
    private readonly IConfiguration _cfg;

    /// <summary>
    /// Creates a new instance of <see cref="MinioReportStorage"/>.
    /// </summary>
    public MinioReportStorage(IMinioClientFactory factory, IConfiguration cfg)
    {
        _factory = factory;
        _cfg = cfg;
    }

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var client = _factory.Create();
        var bucket = _cfg["Storage:Bucket"] ?? "reports";

        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
        }
    }

    /// <inheritdoc />
    public async Task<(string bucket, string objectKey, long sizeBytes, string contentType)> UploadAsync(
        Stream data,
        string fileName,
        string contentType,
        string patientSub,
        Guid reportId,
        CancellationToken ct)
    {
        var client = _factory.Create();
        var bucket = _cfg["Storage:Bucket"] ?? "reports";
        var safeFile = SanitizeFileName(fileName);
        var objectKey = $"{patientSub}/{reportId}/{safeFile}";
        var size = data.Length;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType);

        await client.PutObjectAsync(putArgs, ct);

        return (bucket, objectKey, size, contentType);
    }

    /// <inheritdoc />
    public async Task<(Stream stream, string contentType, string fileName)> DownloadAsync(
        string bucket,
        string objectKey,
        string fileName,
        CancellationToken ct)
    {
        var client = _factory.Create();
        var ms = new MemoryStream();

        var stat = await client.StatObjectAsync(
            new StatObjectArgs().WithBucket(bucket).WithObject(objectKey),
            ct);

        var getArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(ms));

        await client.GetObjectAsync(getArgs, ct);
        ms.Position = 0;

        return (ms, stat.ContentType ?? "application/octet-stream", fileName);
    }

    /// <summary>
    /// Replaces invalid filesystem characters in the uploaded file name.
    /// </summary>
    /// <remarks>
    /// This is used to generate object keys that are safe and predictable.
    /// </remarks>
    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');

        return fileName;
    }
}