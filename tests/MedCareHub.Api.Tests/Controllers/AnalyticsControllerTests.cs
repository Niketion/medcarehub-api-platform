using FluentAssertions;
using MedCareHub.Api.Controllers;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Models;
using MedCareHub.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MedCareHub.Api.Tests.Controllers;

public sealed class AnalyticsControllerTests
{
    [Fact]
    public async Task GetEconomics_ShouldCalculateTotals_AndGroupings()
    {
        await using var db = TestDbFactory.Create();

        var prest1 = new Prestazione
        {
            Id = Guid.NewGuid(),
            Name = "Visita cardiologica",
            BasePrice = 100m
        };

        var prest2 = new Prestazione
        {
            Id = Guid.NewGuid(),
            Name = "Analisi del sangue",
            BasePrice = 50m
        };

        var slot1 = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-rossi",
            PrestazioneId = prest1.Id,
            Prestazione = prest1,
            StartsAt = new DateTimeOffset(2026, 04, 01, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 04, 01, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Booked
        };

        var slot2 = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-rossi",
            PrestazioneId = prest2.Id,
            Prestazione = prest2,
            StartsAt = new DateTimeOffset(2026, 04, 02, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 04, 02, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Booked
        };

        var slot3 = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-bianchi",
            PrestazioneId = prest1.Id,
            Prestazione = prest1,
            StartsAt = new DateTimeOffset(2026, 04, 03, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 04, 03, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Booked
        };

        db.Prestazioni.AddRange(prest1, prest2);
        db.Slots.AddRange(slot1, slot2, slot3);
        db.Bookings.AddRange(
            new Booking
            {
                Id = Guid.NewGuid(),
                PatientSub = "patient-1",
                SlotId = slot1.Id,
                Slot = slot1,
                Status = BookingStatus.Confirmed,
                BookedPrice = 100m,
                CreatedAt = new DateTimeOffset(2026, 04, 01, 8, 0, 0, TimeSpan.Zero)
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                PatientSub = "patient-2",
                SlotId = slot2.Id,
                Slot = slot2,
                Status = BookingStatus.Completed,
                BookedPrice = 50m,
                CreatedAt = new DateTimeOffset(2026, 04, 02, 8, 0, 0, TimeSpan.Zero)
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                PatientSub = "patient-3",
                SlotId = slot3.Id,
                Slot = slot3,
                Status = BookingStatus.Completed,
                BookedPrice = 100m,
                CreatedAt = new DateTimeOffset(2026, 04, 03, 8, 0, 0, TimeSpan.Zero)
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                PatientSub = "patient-4",
                SlotId = slot3.Id,
                Slot = slot3,
                Status = BookingStatus.Cancelled,
                BookedPrice = 100m,
                CreatedAt = new DateTimeOffset(2026, 04, 03, 7, 0, 0, TimeSpan.Zero)
            });

        await db.SaveChangesAsync();

        var sut = new AnalyticsController(db);

        var result = await sut.GetEconomics(null, null, null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<DashboardEconomicsDto>().Subject;

        dto.EstimatedRevenue.Should().Be(100m);
        dto.RealizedRevenue.Should().Be(150m);
        dto.ConfirmedBookings.Should().Be(1);
        dto.CompletedBookings.Should().Be(2);
        dto.AverageTicket.Should().BeApproximately(83.3333333333m, 0.001m);

        dto.ByDoctor.Should().Contain(x =>
            x.DoctorId == "dr-rossi" &&
            x.ConfirmedBookings == 1 &&
            x.CompletedBookings == 1 &&
            x.EstimatedRevenue == 100m &&
            x.RealizedRevenue == 50m);

        dto.ByDoctor.Should().Contain(x =>
            x.DoctorId == "dr-bianchi" &&
            x.ConfirmedBookings == 0 &&
            x.CompletedBookings == 1 &&
            x.EstimatedRevenue == 0m &&
            x.RealizedRevenue == 100m);

        dto.ByPrestazione.Should().Contain(x =>
            x.PrestazioneName == "Visita cardiologica" &&
            x.ConfirmedBookings == 1 &&
            x.CompletedBookings == 1 &&
            x.EstimatedRevenue == 100m &&
            x.RealizedRevenue == 100m);

        dto.ByPrestazione.Should().Contain(x =>
            x.PrestazioneName == "Analisi del sangue" &&
            x.ConfirmedBookings == 0 &&
            x.CompletedBookings == 1 &&
            x.EstimatedRevenue == 0m &&
            x.RealizedRevenue == 50m);
    }

    [Fact]
    public async Task GetEconomics_ShouldFilterByDoctor_AndDateRange()
    {
        await using var db = TestDbFactory.Create();

        var prest = new Prestazione
        {
            Id = Guid.NewGuid(),
            Name = "Visita cardiologica",
            BasePrice = 100m
        };

        var slotIn = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-rossi",
            PrestazioneId = prest.Id,
            Prestazione = prest,
            StartsAt = new DateTimeOffset(2026, 04, 01, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 04, 01, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Booked
        };

        var slotOut = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-bianchi",
            PrestazioneId = prest.Id,
            Prestazione = prest,
            StartsAt = new DateTimeOffset(2026, 05, 01, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 05, 01, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Booked
        };

        db.Prestazioni.Add(prest);
        db.Slots.AddRange(slotIn, slotOut);
        db.Bookings.AddRange(
            new Booking
            {
                Id = Guid.NewGuid(),
                PatientSub = "patient-1",
                SlotId = slotIn.Id,
                Slot = slotIn,
                Status = BookingStatus.Confirmed,
                BookedPrice = 100m
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                PatientSub = "patient-2",
                SlotId = slotOut.Id,
                Slot = slotOut,
                Status = BookingStatus.Completed,
                BookedPrice = 100m
            });

        await db.SaveChangesAsync();

        var sut = new AnalyticsController(db);

        var result = await sut.GetEconomics(
            new DateTimeOffset(2026, 04, 01, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 04, 30, 23, 59, 59, TimeSpan.Zero),
            "rossi",
            CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<DashboardEconomicsDto>().Subject;

        dto.EstimatedRevenue.Should().Be(100m);
        dto.RealizedRevenue.Should().Be(0m);
        dto.ByDoctor.Should().ContainSingle(x => x.DoctorId == "dr-rossi");
        dto.ByDoctor.Should().NotContain(x => x.DoctorId == "dr-bianchi");
    }
}