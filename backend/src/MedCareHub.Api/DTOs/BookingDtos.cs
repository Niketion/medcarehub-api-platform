namespace MedCareHub.Api.DTOs;

public sealed record BookingDto(
    Guid Id,
    Guid SlotId,
    DateTimeOffset SlotStartsAt,
    DateTimeOffset SlotEndsAt,
    string SlotDoctorId,
    Guid? SlotPrestazioneId,
    string? SlotPrestazioneName,
    decimal BookedPrice,
    string Status,
    string PaymentStatus,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt
);

public sealed record CreateBookingRequest(Guid SlotId);