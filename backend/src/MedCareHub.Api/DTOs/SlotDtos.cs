namespace MedCareHub.Api.DTOs;

public sealed record SlotDto(
    Guid Id,
    string DoctorId,
    Guid? PrestazioneId,
    string? PrestazioneName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status
);

public sealed record CreateSlotRequest(
    string DoctorId,
    Guid? PrestazioneId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt
);