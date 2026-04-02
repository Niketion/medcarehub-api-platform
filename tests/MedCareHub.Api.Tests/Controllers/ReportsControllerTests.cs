using System.Text;
using FluentAssertions;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Controllers;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Models;
using MedCareHub.Api.Storage;
using MedCareHub.Api.Tests.TestHelpers;
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
        IFormFile file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "report.txt")
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

        var request = new UploadReportRequest
        {
            BookingId = booking.Id,
            File = file
        };

        var result = await sut.Upload(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await storage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFilePretendsToBePdf_ButHeaderIsInvalid()
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

        var fakePdfBytes = Encoding.UTF8.GetBytes("not-a-real-pdf");
        IFormFile file = new FormFile(new MemoryStream(fakePdfBytes), 0, fakePdfBytes.Length, "file", "report.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
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

        var request = new UploadReportRequest
        {
            BookingId = booking.Id,
            File = file
        };

        var result = await sut.Upload(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await storage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Upload_ShouldSetSignedAt_WhenUploaderIsDoctor()
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

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 test");
        IFormFile file = new FormFile(new MemoryStream(pdfBytes), 0, pdfBytes.Length, "file", "report.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var storage = Substitute.For<IReportStorage>();
        storage.UploadAsync(
                Arg.Any<Stream>(),
                "report.pdf",
                "application/pdf",
                "patient-1",
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("reports", "patient-1/report-id/report.pdf", (long)pdfBytes.Length, "application/pdf")));

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

        var request = new UploadReportRequest
        {
            BookingId = booking.Id,
            File = file,
            ReportType = "Dimissione",
            DocumentDate = new DateTimeOffset(2026, 04, 01, 0, 0, 0, TimeSpan.Zero)
        };

        var result = await sut.Upload(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ReportDto>().Subject;

        dto.AuthorRole.Should().Be(Roles.Doctor);
        dto.SignedAt.Should().NotBeNull();

        db.Reports.Should().ContainSingle();
        var fakeAudit = audit;
        fakeAudit.Calls.Should().ContainSingle(x =>
            x.Event == "report_uploaded" &&
            x.Outcome == AuditOutcome.Success &&
            x.ActorSub == "doctor-1");
    }

    [Fact]
    public async Task Download_ShouldReturnForbid_WhenPatientIsNotOwner_AndWriteFailAudit()
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
            Status = BookingStatus.Completed
        };

        var report = new Report
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            PatientSub = "patient-1",
            Bucket = "reports",
            ObjectKey = "patient-1/report-id/report.pdf",
            FileName = "report.pdf",
            ContentType = "application/pdf",
            SizeBytes = 128
        };

        db.Slots.Add(slot);
        db.Bookings.Add(booking);
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var storage = Substitute.For<IReportStorage>();
        var audit = new FakeAuditService();

        var sut = new ReportsController(db, storage, audit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("patient-2", Roles.Patient)
                }
            }
        };

        var result = await sut.Download(report.Id, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        await storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default!, default!, default);

        audit.Calls.Should().ContainSingle(x =>
            x.Event == "report_download_denied" &&
            x.Outcome == AuditOutcome.Fail &&
            x.ResourceId == report.Id.ToString());
    }

    [Fact]
    public async Task Download_ShouldAllowOwner_AndWriteSuccessAudit()
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
            Status = BookingStatus.Completed
        };

        var report = new Report
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            PatientSub = "patient-1",
            Bucket = "reports",
            ObjectKey = "patient-1/report-id/report.pdf",
            FileName = "report.pdf",
            ContentType = "application/pdf",
            SizeBytes = 128
        };

        db.Slots.Add(slot);
        db.Bookings.Add(booking);
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var storage = Substitute.For<IReportStorage>();
        storage.DownloadAsync(
                "reports",
                "patient-1/report-id/report.pdf",
                "report.pdf",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(Stream stream, string contentType, string fileName)>(
                (new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4")), "application/pdf", "report.pdf")));

        var audit = new FakeAuditService();

        var sut = new ReportsController(db, storage, audit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("patient-1", Roles.Patient)
                }
            }
        };

        var result = await sut.Download(report.Id, CancellationToken.None);

        var file = result.Should().BeOfType<FileStreamResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().Be("report.pdf");

        audit.Calls.Should().ContainSingle(x =>
            x.Event == "report_downloaded" &&
            x.Outcome == AuditOutcome.Success &&
            x.ResourceId == report.Id.ToString());
    }
}