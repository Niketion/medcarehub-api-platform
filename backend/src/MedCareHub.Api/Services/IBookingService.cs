using MedCareHub.Api.Models;

namespace MedCareHub.Api.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(string patientSub, Guid slotId, CancellationToken ct);
    Task CancelBookingAsync(string patientSub, Guid bookingId, CancellationToken ct);
    Task CompleteBookingAsync(Guid bookingId, CancellationToken ct);
}