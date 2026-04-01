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

            await _audit.LogAsync(
                "booking_created",
                patientSub,
                actorRole,
                AuditOutcome.Success,
                "booking",
                booking.Id.ToString(),
                new { booking.SlotId, booking.BookedPrice },
                ct);

            return Ok(ToDto(booking));
        }
        catch (NotFoundException)
        {
            await _audit.LogAsync("booking_create_failed", patientSub, actorRole, AuditOutcome.Fail, "slot", req.SlotId.ToString(), new { reason = "slot_not_found" }, ct);
            throw;
        }
        catch (ConflictException ex)
        {
            await _audit.LogAsync("booking_create_failed", patientSub, actorRole, AuditOutcome.Fail, "slot", req.SlotId.ToString(), new { reason = ex.Message }, ct);
            throw;
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.Patient)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var patientSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = Roles.Patient;

        try
        {
            await _bookingService.CancelBookingAsync(patientSub, id, ct);

            await _audit.LogAsync(
                "booking_cancelled",
                patientSub,
                actorRole,
                AuditOutcome.Success,
                "booking",
                id.ToString(),
                null,
                ct);

            return NoContent();
        }
        catch (Exception ex) when (ex is NotFoundException or ConflictException or ForbiddenException)
        {
            await _audit.LogAsync(
                "booking_cancel_failed",
                patientSub,
                actorRole,
                AuditOutcome.Fail,
                "booking",
                id.ToString(),
                new { reason = ex.Message },
                ct);

            throw;
        }
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.Staff)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var actorSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        try
        {
            await _bookingService.CompleteBookingAsync(id, ct);

            await _audit.LogAsync(
                "booking_completed",
                actorSub,
                actorRole,
                AuditOutcome.Success,
                "booking",
                id.ToString(),
                null,
                ct);

            return NoContent();
        }
        catch (Exception ex) when (ex is NotFoundException or ConflictException)
        {
            await _audit.LogAsync(
                "booking_complete_failed",
                actorSub,
                actorRole,
                AuditOutcome.Fail,
                "booking",
                id.ToString(),
                new { reason = ex.Message },
                ct);

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
            .Take(500)
            .ToListAsync(ct);

        return Ok(items.Select(ToDto));
    }

    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Bookings.AsNoTracking()
            .Include(b => b.Slot)
            .ThenInclude(s => s.Prestazione)
            .OrderByDescending(b => b.CreatedAt)
            .Take(1000)
            .ToListAsync(ct);

        return Ok(items.Select(ToDto));
    }

    [HttpPost("{id:guid}/mark-paid")]
    [Authorize(Policy = Policies.Staff)]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
    {
        var actorSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        try
        {
            await _bookingService.MarkBookingPaidAsync(id, ct);

            await _audit.LogAsync(
                "booking_paid",
                actorSub,
                actorRole,
                AuditOutcome.Success,
                "booking",
                id.ToString(),
                null,
                ct);

            return NoContent();
        }
        catch (Exception ex) when (ex is NotFoundException or ConflictException)
        {
            await _audit.LogAsync(
                "booking_pay_failed",
                actorSub,
                actorRole,
                AuditOutcome.Fail,
                "booking",
                id.ToString(),
                new { reason = ex.Message },
                ct);

            throw;
        }
    }

    private static BookingDto ToDto(Booking b) => new(
        b.Id,
        b.SlotId,
        b.Slot.StartsAt,
        b.Slot.EndsAt,
        b.Slot.DoctorId,
        b.Slot.PrestazioneId,
        b.Slot.Prestazione?.Name,
        b.BookedPrice,
        b.Status,
        b.PaymentStatus,
        b.PaidAt,
        b.CreatedAt
    );
}