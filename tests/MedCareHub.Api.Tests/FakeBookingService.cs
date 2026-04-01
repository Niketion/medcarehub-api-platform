using MedCareHub.Api.Models;
using MedCareHub.Api.Services;

namespace MedCareHub.Api.Tests.TestHelpers;

public sealed class FakeBookingService : IBookingService
{
    public Func<string, Guid, CancellationToken, Task<Booking>>? OnCreate { get; set; }
    public Func<string, Guid, CancellationToken, Task>? OnCancel { get; set; }
    public Func<Guid, CancellationToken, Task>? OnComplete { get; set; }

    public Task<Booking> CreateBookingAsync(string patientSub, Guid slotId, CancellationToken ct)
        => OnCreate is not null
            ? OnCreate(patientSub, slotId, ct)
            : Task.FromResult(new Booking());

    public Task CancelBookingAsync(string patientSub, Guid bookingId, CancellationToken ct)
        => OnCancel is not null
            ? OnCancel(patientSub, bookingId, ct)
            : Task.CompletedTask;

    public Task CompleteBookingAsync(Guid bookingId, CancellationToken ct)
        => OnComplete is not null
            ? OnComplete(bookingId, ct)
            : Task.CompletedTask;
}