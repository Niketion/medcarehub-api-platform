namespace MedCareHub.Api.DTOs;

public sealed record ReportDto(
    Guid Id,
    Guid BookingId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? ReportType,
    DateTimeOffset? DocumentDate,
    string? AuthorSub,
    string? AuthorRole,
    DateTimeOffset? SignedAt,
    DateTimeOffset CreatedAt
);