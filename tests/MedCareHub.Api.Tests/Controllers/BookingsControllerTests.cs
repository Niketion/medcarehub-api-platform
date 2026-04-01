using FluentAssertions;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Controllers;
using MedCareHub.Api.Data;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;
using MedCareHub.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MedCareHub.Api.Tests.Controllers;

public sealed class BookingsControllerTests
{
    [Fact]
    public async Task Create_ShouldReturnBookedPrice_AndWriteSuccessAudit()
    {
        await using var db = TestDbFactory.Create();

        var prestazione = new Prestazione
        {
            Id = Guid.NewGuid(),
            Name = "Visita cardiologica",
            BasePrice = 120m
        };

        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-rossi",
            PrestazioneId = prestazione.Id,
            Prestazione = prestazione,
            StartsAt = new DateTimeOffset(2026, 04, 01, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 04, 01, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Booked
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            PatientSub = "patient-1",
            SlotId = slot.Id,
            Slot = slot,
            Status = BookingStatus.Confirmed,
            BookedPrice = 120m
        };

        db.Prestazioni.Add(prestazione);
        db.Slots.Add(slot);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var fakeService = new FakeBookingService
        {
            OnCreate = (_, _, _) => Task.FromResult(booking)
        };
        var fakeAudit = new FakeAuditService();

        var sut = new BookingsController(db, fakeService, fakeAudit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("patient-1", Roles.Patient)
                }
            }
        };

        var result = await sut.Create(new CreateBookingRequest(slot.Id), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<BookingDto>().Subject;

        dto.BookedPrice.Should().Be(120m);
        dto.SlotPrestazioneName.Should().Be("Visita cardiologica");

        fakeAudit.Calls.Should().ContainSingle(x =>
            x.Event == "booking_created" &&
            x.Outcome == AuditOutcome.Success &&
            x.ActorSub == "patient-1");
    }

    [Fact]
    public async Task Create_ShouldAuditFailure_WhenServiceThrowsConflict()
    {
        await using var db = TestDbFactory.Create();

        var slotId = Guid.NewGuid();

        var fakeService = new FakeBookingService
        {
            OnCreate = (_, _, _) => throw new ConflictException("Slot not available.")
        };
        var fakeAudit = new FakeAuditService();

        var sut = new BookingsController(db, fakeService, fakeAudit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("patient-1", Roles.Patient)
                }
            }
        };

        var act = async () => await sut.Create(new CreateBookingRequest(slotId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        fakeAudit.Calls.Should().ContainSingle(x =>
            x.Event == "booking_create_failed" &&
            x.Outcome == AuditOutcome.Fail &&
            x.ResourceType == "slot");
    }

    [Fact]
    public async Task Cancel_ShouldReturnNoContent_AndWriteSuccessAudit()
    {
        await using var db = TestDbFactory.Create();

        var bookingId = Guid.NewGuid();
        var called = false;

        var fakeService = new FakeBookingService
        {
            OnCancel = (_, id, _) =>
            {
                called = id == bookingId;
                return Task.CompletedTask;
            }
        };
        var fakeAudit = new FakeAuditService();

        var sut = new BookingsController(db, fakeService, fakeAudit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("patient-1", Roles.Patient)
                }
            }
        };

        var result = await sut.Cancel(bookingId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        called.Should().BeTrue();

        fakeAudit.Calls.Should().ContainSingle(x =>
            x.Event == "booking_cancelled" &&
            x.Outcome == AuditOutcome.Success &&
            x.ResourceId == bookingId.ToString());
    }
}