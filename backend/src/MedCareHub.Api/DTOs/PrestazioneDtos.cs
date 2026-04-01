namespace MedCareHub.Api.DTOs;

public sealed record PrestazioneDto(
    Guid Id,
    string Name,
    int? DurationMinutes,
    string? Description,
    decimal BasePrice,
    DateTimeOffset CreatedAt
);

public sealed record CreatePrestazioneRequest(
    string Name,
    int? DurationMinutes,
    string? Description,
    decimal BasePrice
);