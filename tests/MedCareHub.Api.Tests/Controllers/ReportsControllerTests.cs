using FluentAssertions;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Controllers;
using MedCareHub.Api.Models;
using MedCareHub.Api.Tests.TestHelpers;
using MedCareHub.Api.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace MedCareHub.Api.Tests.Controllers;

public sealed class ReportsControllerTests
{
    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsNotPdf()
    {
        await using var db = TestDbFactory.Create();

        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-rossi",
            StartsAt = DateTimeOffset.UtcNow,
            EndsAt = DateTimeOffset.UtcNow.AddMinutes(30),
            Status = SlotStatus.Booked
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            PatientSub = "patient-1",
            SlotId = slot.Id,
            Slot = slot,
            Status = BookingStatus.Confirmed
        };

        db.Slots.Add(slot);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var bytes = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "report.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var storage = Substitute.For<IReportStorage>();
        var audit = new FakeAuditService();

        var sut = new ReportsController(db, storage, audit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("doctor-1", Roles.Doctor)
                }
            }
        };

        var request = new MedCareHub.Api.DTOs.UploadReportRequest
        {
            BookingId = booking.Id,
            File = file
        };

        var result = await sut.Upload(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await storage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default!, default, default);
    }
}