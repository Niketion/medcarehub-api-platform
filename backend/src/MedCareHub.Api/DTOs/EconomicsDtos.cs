namespace MedCareHub.Api.DTOs;

public sealed record DashboardEconomicsDto(
    decimal EstimatedRevenue,
    decimal RealizedRevenue,
    decimal AverageTicket,
    int ConfirmedBookings,
    int CompletedBookings,
    IReadOnlyList<DoctorEconomicsDto> ByDoctor,
    IReadOnlyList<PrestazioneEconomicsDto> ByPrestazione
);

public sealed record DoctorEconomicsDto(
    string DoctorId,
    int ConfirmedBookings,
    int CompletedBookings,
    decimal EstimatedRevenue,
    decimal RealizedRevenue
);

public sealed record PrestazioneEconomicsDto(
    Guid? PrestazioneId,
    string PrestazioneName,
    int ConfirmedBookings,
    int CompletedBookings,
    decimal EstimatedRevenue,
    decimal RealizedRevenue
);