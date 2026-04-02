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

/// <summary>
/// Exposes endpoints for booking lifecycle management.
/// </summary>
/// <remarks>
/// Patients can create, cancel and list their own bookings.
/// Staff members can list all bookings, complete a booking and mark it as paid.
/// </remarks>
[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
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

    /// <summary>
    /// Creates a booking for the authenticated patient.
    /// </summary>
    /// <param name="req">Booking creation payload containing the target slot identifier.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>The created booking with slot and service details.</returns>
    /// <response code="200">Booking created successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user is not allowed to create bookings.</response>
    /// <response code="404">The target slot does not exist.</response>
    /// <response code="409">The slot is not available or has already been booked.</response>
    [HttpPost]
    [Authorize(Policy = Policies.Patient)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Cancels a booking owned by the authenticated patient.
    /// </summary>
    /// <param name="id">Identifier of the booking to cancel.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>No content when the cancellation succeeds.</returns>
    /// <response code="204">Booking cancelled successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The booking is not owned by the authenticated patient.</response>
    /// <response code="404">The booking does not exist.</response>
    /// <response code="409">The booking cannot be cancelled.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.Patient)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Marks a booking as completed.
    /// </summary>
    /// <param name="id">Identifier of the booking to complete.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>No content when the operation succeeds.</returns>
    /// <response code="204">Booking completed successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user is not allowed to complete bookings.</response>
    /// <response code="404">The booking does not exist.</response>
    /// <response code="409">The booking cannot be completed.</response>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Returns bookings created by the authenticated patient.
    /// </summary>
    /// <response code="200">Bookings returned successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user is not allowed to access this resource.</response>
    [HttpGet("my")]
    [Authorize(Policy = Policies.Patient)]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

    /// <summary>
    /// Returns all bookings visible to staff users.
    /// </summary>
    /// <response code="200">Bookings returned successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user is not allowed to access this resource.</response>
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

    /// <summary>
    /// Marks a booking as paid and records the payment timestamp.
    /// </summary>
    /// <param name="id">Identifier of the booking to update.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>No content when the operation succeeds.</returns>
    /// <response code="204">Booking marked as paid successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user is not allowed to mark payments.</response>
    /// <response code="404">The booking does not exist.</response>
    /// <response code="409">The booking cannot be marked as paid.</response>
    [HttpPost("{id:guid}/mark-paid")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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