namespace MedCareHub.Api.DTOs;

public sealed record RevenueTrendPointDto(
    string Label,
    decimal RealizedRevenue,
    decimal PaidRevenue
);

public sealed record DashboardEconomicsDto(
    decimal EstimatedRevenue,
    decimal RealizedRevenue,
    decimal PaidRevenue,
    decimal AverageTicket,
    int ConfirmedBookings,
    int CompletedBookings,
    int PaidBookings,
    IReadOnlyList<DoctorEconomicsDto> ByDoctor,
    IReadOnlyList<PrestazioneEconomicsDto> ByPrestazione,
    IReadOnlyList<RevenueTrendPointDto> RevenueTrend
);

public sealed record DoctorEconomicsDto(
    string DoctorId,
    int ConfirmedBookings,
    int CompletedBookings,
    int PaidBookings,
    decimal EstimatedRevenue,
    decimal RealizedRevenue,
    decimal PaidRevenue
);

public sealed record PrestazioneEconomicsDto(
    Guid? PrestazioneId,
    string PrestazioneName,
    int ConfirmedBookings,
    int CompletedBookings,
    int PaidBookings,
    decimal EstimatedRevenue,
    decimal RealizedRevenue,
    decimal PaidRevenue
);