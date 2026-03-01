using MedCareHub.Api.Data;
using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedCareHub.Api.Services;

public sealed class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db) => _db = db;

    public async Task<Booking> CreateBookingAsync(string patientSub, Guid slotId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Lock slot row to avoid races
        var slot = await _db.Slots
            .FromSqlInterpolated($@"SELECT * FROM ""public"".""Slots"" WHERE ""Id"" = {slotId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

        if (slot is null)
            throw new NotFoundException("Slot not found.");

        if (!string.Equals(slot.Status, SlotStatus.Available, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Slot not available.");

        slot.Status = SlotStatus.Booked;

        var booking = new Booking
        {
            PatientSub = patientSub,
            SlotId = slotId,
            Status = BookingStatus.Confirmed
        };

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            throw new ConflictException("Slot already booked.");
        }

        await tx.CommitAsync(ct);
        return booking;
    }

    public async Task CancelBookingAsync(string patientSub, Guid bookingId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var booking = await _db.Bookings
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            throw new NotFoundException("Booking not found.");

        if (!string.Equals(booking.PatientSub, patientSub, StringComparison.Ordinal))
            throw new ForbiddenException("Not owner.");

        if (string.Equals(booking.Status, BookingStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(booking.Status, BookingStatus.Completed, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Completed bookings cannot be cancelled.");

        booking.Status = BookingStatus.Cancelled;

        if (booking.Slot is not null && string.Equals(booking.Slot.Status, SlotStatus.Booked, StringComparison.OrdinalIgnoreCase))
            booking.Slot.Status = SlotStatus.Available;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task CompleteBookingAsync(Guid bookingId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var booking = await _db.Bookings
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            throw new NotFoundException("Booking not found.");

        if (string.Equals(booking.Status, BookingStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Cancelled bookings cannot be completed.");

        if (string.Equals(booking.Status, BookingStatus.Completed, StringComparison.OrdinalIgnoreCase))
            return;

        booking.Status = BookingStatus.Completed;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}