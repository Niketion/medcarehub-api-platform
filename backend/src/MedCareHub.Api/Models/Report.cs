namespace MedCareHub.Api.Models;

public sealed class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string PatientSub { get; set; } = default!;

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public string Bucket { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }

    // Metadata
    public string? ReportType { get; set; }
    public DateTimeOffset? DocumentDate { get; set; }
    public string? AuthorSub { get; set; }
    public string? AuthorRole { get; set; }
    public DateTimeOffset? SignedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}