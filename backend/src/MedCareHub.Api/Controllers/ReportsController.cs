using System.Security.Claims;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Data;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Models;
using MedCareHub.Api.Services;
using MedCareHub.Api.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IReportStorage _storage;
    private readonly IAuditService _audit;

    public ReportsController(AppDbContext db, IReportStorage storage, IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    [HttpPost("upload")]
    [Authorize(Policy = Policies.Staff)]
    [RequestSizeLimit(20_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ReportDto>> Upload(
        [FromForm] UploadReportRequest request,
        CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = "file is required" });

        var booking = await _db.Bookings
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, ct);

        if (booking is null)
            return NotFound(new { error = "booking not found" });

        var patientSub = booking.PatientSub;

        var actorSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        var report = new Report
        {
            BookingId = request.BookingId,
            PatientSub = patientSub,
            FileName = request.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType,

            ReportType = string.IsNullOrWhiteSpace(request.ReportType) ? null : request.ReportType.Trim(),
            DocumentDate = request.DocumentDate,

            AuthorSub = actorSub,
            AuthorRole = actorRole,
            SignedAt = string.Equals(actorRole, Roles.Doctor, StringComparison.OrdinalIgnoreCase) || string.Equals(actorRole, Roles.Admin, StringComparison.OrdinalIgnoreCase)
                ? DateTimeOffset.UtcNow
                : null
        };

        await using var stream = request.File.OpenReadStream();
        var (bucket, objectKey, sizeBytes, contentType) = await _storage.UploadAsync(
            stream, request.File.FileName, report.ContentType, patientSub, report.Id, ct);

        report.Bucket = bucket;
        report.ObjectKey = objectKey;
        report.SizeBytes = sizeBytes;
        report.ContentType = contentType;

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("report_uploaded", actorSub, actorRole, AuditOutcome.Success, "report", report.Id.ToString(),
            new
            {
                request.BookingId,
                patientSub,
                file = report.FileName,
                sizeBytes,
                report.ReportType,
                report.DocumentDate,
                report.SignedAt
            }, ct);

        return Ok(new ReportDto(
            report.Id,
            report.BookingId,
            report.FileName,
            report.ContentType,
            report.SizeBytes,
            report.ReportType,
            report.DocumentDate,
            report.AuthorSub,
            report.AuthorRole,
            report.SignedAt,
            report.CreatedAt
        ));
    }

    [HttpGet("my")]
    [Authorize(Policy = Policies.Patient)]
    public async Task<ActionResult<IEnumerable<ReportDto>>> My(CancellationToken ct)
    {
        var patientSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";

        var items = await _db.Reports.AsNoTracking()
            .Where(r => r.PatientSub == patientSub)
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        return Ok(items.Select(r => new ReportDto(
            r.Id, r.BookingId, r.FileName, r.ContentType, r.SizeBytes,
            r.ReportType, r.DocumentDate, r.AuthorSub, r.AuthorRole, r.SignedAt,
            r.CreatedAt
        )));
    }

    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetAll([FromQuery] Guid? bookingId, CancellationToken ct)
    {
        var q = _db.Reports.AsNoTracking().AsQueryable();

        if (bookingId.HasValue)
            q = q.Where(r => r.BookingId == bookingId.Value);

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        return Ok(items.Select(r => new ReportDto(
            r.Id, r.BookingId, r.FileName, r.ContentType, r.SizeBytes,
            r.ReportType, r.DocumentDate, r.AuthorSub, r.AuthorRole, r.SignedAt,
            r.CreatedAt
        )));
    }

    [HttpGet("{id:guid}/download")]
    [Authorize]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken ct)
    {
        var report = await _db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (report is null)
            return NotFound();

        var sub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isStaff = roles.Contains(Roles.Operator) || roles.Contains(Roles.Doctor) || roles.Contains(Roles.Admin);
        var isOwner = string.Equals(report.PatientSub, sub, StringComparison.Ordinal);

        if (!isOwner && !isStaff)
        {
            var actorRole = roles.FirstOrDefault();
            await _audit.LogAsync("report_download_denied", sub, actorRole, AuditOutcome.Fail, "report", report.Id.ToString(),
                new { owner = report.PatientSub }, ct);
            return Forbid();
        }

        var (stream, contentType, fileName) = await _storage.DownloadAsync(report.Bucket, report.ObjectKey, report.FileName, ct);

        var role = roles.FirstOrDefault();
        await _audit.LogAsync("report_downloaded", sub, role, AuditOutcome.Success, "report", report.Id.ToString(),
            new { owner = report.PatientSub }, ct);

        return File(stream, contentType, fileName);
    }
}