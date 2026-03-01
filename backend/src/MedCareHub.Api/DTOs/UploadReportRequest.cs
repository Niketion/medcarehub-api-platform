namespace MedCareHub.Api.DTOs;

public sealed class UploadReportRequest
{
    public Guid BookingId { get; set; }

    public string? ReportType { get; set; }
    public DateTimeOffset? DocumentDate { get; set; }

    public IFormFile File { get; set; } = default!;
}