using System.Security.Claims;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Data;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;
using MedCareHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBookingService _bookingService;
    private readonly IAuditService _audit;

    public BookingsController(AppDbContext db, IBookingService bookingService, IAuditService audit)
    {
        _db = db;
        _bookingService = bookingService;
        _audit = audit;
    }

    [HttpPost]
    [Authorize(Policy = Policies.Patient)]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest req, CancellationToken ct)
    {
        var patientSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = Roles.Patient;

        try
        {
            var booking = await _bookingService.CreateBookingAsync(patientSub, req.SlotId, ct);

            booking = await _db.Bookings.AsNoTracking()
                .Include(b => b.Slot)
                .ThenInclude(s => s.Prestazione)
                .FirstAsync(b => b.Id == booking.Id, ct);

            await _audit.LogAsync("booking_created", patientSub, actorRole, AuditOutcome.Success, "booking", booking.Id.ToString(),
                new { slotId = req.SlotId }, ct);

            return Ok(new BookingDto(
                booking.Id,
                booking.SlotId,
                booking.Slot.StartsAt,
                booking.Slot.EndsAt,
                booking.Slot.DoctorId,
                booking.Slot.PrestazioneId,
                booking.Slot.Prestazione?.Name,
                booking.Status,
                booking.CreatedAt
            ));
        }
        catch (ApiException ex) when (ex is ConflictException or NotFoundException or BadRequestException)
        {
            await _audit.LogAsync("booking_create_failed", patientSub, actorRole, AuditOutcome.Fail, "slot", req.SlotId.ToString(),
                new { reason = ex.Message }, ct);
            throw;
        }
    }

    [HttpGet("my")]
    [Authorize(Policy = Policies.Patient)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> My(CancellationToken ct)
    {
        var patientSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";

        var items = await _db.Bookings.AsNoTracking()
            .Include(b => b.Slot)
            .ThenInclude(s => s.Prestazione)
            .Where(b => b.PatientSub == patientSub)
            .OrderByDescending(b => b.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        return Ok(items.Select(b => new BookingDto(
            b.Id, b.SlotId, b.Slot.StartsAt, b.Slot.EndsAt, b.Slot.DoctorId,
            b.Slot.PrestazioneId, b.Slot.Prestazione?.Name,
            b.Status, b.CreatedAt
        )));
    }

    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var q = _db.Bookings
            .Include(b => b.Slot)
            .ThenInclude(s => s.Prestazione)
            .AsNoTracking()
            .AsQueryable();

        if (from.HasValue)
            q = q.Where(b => b.Slot.StartsAt >= from.Value);

        if (to.HasValue)
            q = q.Where(b => b.Slot.EndsAt <= to.Value);

        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        return Ok(items.Select(b => new BookingDto(
            b.Id,
            b.SlotId,
            b.Slot.StartsAt,
            b.Slot.EndsAt,
            b.Slot.DoctorId,
            b.Slot.PrestazioneId,
            b.Slot.Prestazione?.Name,
            b.Status,
            b.CreatedAt
        )));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.Patient)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken ct)
    {
        var patientSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";

        try
        {
            await _bookingService.CancelBookingAsync(patientSub, id, ct);
            await _audit.LogAsync("booking_cancelled", patientSub, Roles.Patient, AuditOutcome.Success, "booking", id.ToString(), null, ct);
            return NoContent();
        }
        catch (ApiException ex)
        {
            await _audit.LogAsync("booking_cancel_failed", patientSub, Roles.Patient, AuditOutcome.Fail, "booking", id.ToString(),
                new { reason = ex.Message }, ct);
            throw;
        }
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.Staff)]
    public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken ct)
    {
        var actorSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        try
        {
            await _bookingService.CompleteBookingAsync(id, ct);

            await _audit.LogAsync("booking_completed", actorSub, actorRole, AuditOutcome.Success, "booking", id.ToString(), null, ct);
            return NoContent();
        }
        catch (ApiException ex)
        {
            await _audit.LogAsync("booking_complete_failed", actorSub, actorRole, AuditOutcome.Fail, "booking", id.ToString(),
                new { reason = ex.Message }, ct);
            throw;
        }
    }
}