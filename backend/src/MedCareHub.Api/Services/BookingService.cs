using MedCareHub.Api.Data;
using MedCareHub.Api.Domain;
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

        var slot = await _db.Slots
            .FromSqlInterpolated($@"SELECT * FROM ""public"".""Slots"" WHERE ""Id"" = {slotId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

        if (slot is null)
            throw new NotFoundException("Slot not found.");

        BookingRules.EnsureSlotCanBeBooked(slot);

        decimal bookedPrice = 0m;

        if (slot.PrestazioneId.HasValue)
        {
            bookedPrice = await _db.Prestazioni
                .AsNoTracking()
                .Where(p => p.Id == slot.PrestazioneId.Value)
                .Select(p => p.BasePrice)
                .FirstOrDefaultAsync(ct);
        }

        slot.Status = SlotStatus.Booked;

        var booking = new Booking
        {
            PatientSub = patientSub,
            SlotId = slotId,
            Status = BookingStatus.Confirmed,
            BookedPrice = bookedPrice
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

        BookingRules.EnsurePatientCanCancel(booking, patientSub);

        var changed = BookingRules.TryCancel(booking);
        if (!changed)
        {
            await tx.CommitAsync(ct);
            return;
        }

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

        var changed = BookingRules.TryComplete(booking);
        if (!changed)
        {
            await tx.CommitAsync(ct);
            return;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}