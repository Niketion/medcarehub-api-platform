namespace MedCareHub.Api.Models;

/// <summary>
/// Represents a clinical report file associated with a booking.
/// </summary>
/// <remarks>
/// Report content is stored in object storage, while this entity keeps
/// the metadata required for retrieval, access control and auditability.
/// </remarks>
public sealed class Report
{
    /// <summary>
    /// Report identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable identifier of the patient who owns the report.
    /// </summary>
    public string PatientSub { get; set; } = default!;

    /// <summary>
    /// Identifier of the related booking.
    /// </summary>
    public Guid BookingId { get; set; }

    /// <summary>
    /// Navigation property to the related booking.
    /// </summary>
    public Booking Booking { get; set; } = default!;

    /// <summary>
    /// Storage bucket containing the physical file.
    /// </summary>
    public string Bucket { get; set; } = default!;

    /// <summary>
    /// Storage object key used to retrieve the physical file.
    /// </summary>
    public string ObjectKey { get; set; } = default!;

    /// <summary>
    /// Original logical file name exposed to clients.
    /// </summary>
    public string FileName { get; set; } = default!;

    /// <summary>
    /// MIME type returned during download.
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>
    /// Persisted file size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Optional business classification of the report.
    /// </summary>
    public string? ReportType { get; set; }

    /// <summary>
    /// Optional document date associated with the report content.
    /// </summary>
    public DateTimeOffset? DocumentDate { get; set; }

    /// <summary>
    /// Stable identifier of the user who uploaded the report.
    /// </summary>
    public string? AuthorSub { get; set; }

    /// <summary>
    /// Role used by the uploader when the report was created.
    /// </summary>
    public string? AuthorRole { get; set; }

    /// <summary>
    /// Signature timestamp for flows where upload implies signature.
    /// </summary>
    public DateTimeOffset? SignedAt { get; set; }

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}