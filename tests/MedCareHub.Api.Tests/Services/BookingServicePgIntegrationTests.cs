using FluentAssertions;
using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;
using MedCareHub.Api.Services;
using MedCareHub.Api.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MedCareHub.Api.Tests.Services;

public sealed class BookingServicePgIntegrationTests
{
    [Fact]
    public async Task CreateBookingAsync_ShouldAllowOnlyOneConcurrentBooking_ForSameSlot()
    {
        await using var pg = await PostgresTestDatabase.CreateAsync();

        var prestazioneId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await using (var seedDb = pg.CreateDbContext())
        {
            seedDb.Prestazioni.Add(new Prestazione
            {
                Id = prestazioneId,
                Name = "Visita cardiologica",
                BasePrice = 120m
            });

            seedDb.Slots.Add(new Slot
            {
                Id = slotId,
                DoctorId = "dr-rossi",
                PrestazioneId = prestazioneId,
                StartsAt = new DateTimeOffset(2026, 04, 10, 9, 0, 0, TimeSpan.Zero),
                EndsAt = new DateTimeOffset(2026, 04, 10, 9, 30, 0, TimeSpan.Zero),
                Status = SlotStatus.Available
            });

            await seedDb.SaveChangesAsync();
        }

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var t1 = Task.Run(() => AttemptBookingAsync(pg.ConnectionString, "patient-1", slotId, gate.Task));
        var t2 = Task.Run(() => AttemptBookingAsync(pg.ConnectionString, "patient-2", slotId, gate.Task));

        gate.SetResult();

        var results = await Task.WhenAll(t1, t2);

        results.Count(x => x.Succeeded).Should().Be(1);
        results.Count(x => !x.Succeeded && x.Exception is ConflictException).Should().Be(1);

        var failure = results.Single(x => !x.Succeeded);
        failure.Exception.Should().BeOfType<ConflictException>();
        failure.Exception!.Message.Should().Match(msg =>
            msg == "Slot not available." || msg == "Slot already booked.");

        await using var assertDb = pg.CreateDbContext();

        var bookings = await assertDb.Bookings
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        bookings.Should().HaveCount(1);
        bookings[0].PatientSub.Should().BeOneOf("patient-1", "patient-2");
        bookings[0].Status.Should().Be(BookingStatus.Confirmed);
        bookings[0].BookedPrice.Should().Be(120m);
        bookings[0].PaymentStatus.Should().Be(PaymentStatuses.Unpaid);

        var slot = await assertDb.Slots
            .AsNoTracking()
            .SingleAsync(x => x.Id == slotId);

        slot.Status.Should().Be(SlotStatus.Booked);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldAllowRebooking_AfterCancellation_BecauseUniqueIndexIsFiltered()
    {
        await using var pg = await PostgresTestDatabase.CreateAsync();

        var slotId = Guid.NewGuid();

        await using (var seedDb = pg.CreateDbContext())
        {
            seedDb.Slots.Add(new Slot
            {
                Id = slotId,
                DoctorId = "dr-bianchi",
                StartsAt = new DateTimeOffset(2026, 04, 11, 10, 0, 0, TimeSpan.Zero),
                EndsAt = new DateTimeOffset(2026, 04, 11, 10, 30, 0, TimeSpan.Zero),
                Status = SlotStatus.Available
            });

            await seedDb.SaveChangesAsync();
        }

        Guid firstBookingId;

        await using (var db = pg.CreateDbContext())
        {
            var service = new BookingService(db);
            var first = await service.CreateBookingAsync("patient-1", slotId, CancellationToken.None);
            firstBookingId = first.Id;
        }

        await using (var db = pg.CreateDbContext())
        {
            var service = new BookingService(db);
            await service.CancelBookingAsync("patient-1", firstBookingId, CancellationToken.None);
        }

        await using (var db = pg.CreateDbContext())
        {
            var service = new BookingService(db);
            var second = await service.CreateBookingAsync("patient-2", slotId, CancellationToken.None);

            second.PatientSub.Should().Be("patient-2");
            second.Status.Should().Be(BookingStatus.Confirmed);
        }

        await using var assertDb = pg.CreateDbContext();

        var bookings = await assertDb.Bookings
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        bookings.Should().HaveCount(2);
        bookings.Count(x => x.Status == BookingStatus.Cancelled).Should().Be(1);
        bookings.Count(x => x.Status == BookingStatus.Confirmed).Should().Be(1);

        var slot = await assertDb.Slots
            .AsNoTracking()
            .SingleAsync(x => x.Id == slotId);

        slot.Status.Should().Be(SlotStatus.Booked);
    }

    private static async Task<BookingAttemptResult> AttemptBookingAsync(
        string connectionString,
        string patientSub,
        Guid slotId,
        Task gate)
    {
        await gate;

        await using var db = CreateDbContext(connectionString);
        var service = new BookingService(db);

        try
        {
            var booking = await service.CreateBookingAsync(patientSub, slotId, CancellationToken.None);
            return BookingAttemptResult.Success(booking.Id);
        }
        catch (Exception ex)
        {
            return BookingAttemptResult.Fail(ex);
        }
    }

    private static MedCareHub.Api.Data.AppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MedCareHub.Api.Data.AppDbContext>()
            .UseNpgsql(connectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        return new MedCareHub.Api.Data.AppDbContext(options);
    }

    private sealed record BookingAttemptResult(bool Succeeded, Guid? BookingId, Exception? Exception)
    {
        public static BookingAttemptResult Success(Guid bookingId) => new(true, bookingId, null);
        public static BookingAttemptResult Fail(Exception ex) => new(false, null, ex);
    }
}