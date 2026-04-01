using MedCareHub.Api.Auth;
using MedCareHub.Api.Data;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AnalyticsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("economics")]
    [Authorize(Policy = Policies.Staff)]
    public async Task<ActionResult<DashboardEconomicsDto>> GetEconomics(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? doctorId,
        CancellationToken ct)
    {
        var q = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Slot)
            .ThenInclude(s => s.Prestazione)
            .AsQueryable();

        if (from.HasValue)
            q = q.Where(b => b.Slot.StartsAt >= from.Value);

        if (to.HasValue)
            q = q.Where(b => b.Slot.EndsAt <= to.Value);

        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Take(5000)
            .ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            items = items
                .Where(b => (b.Slot.DoctorId ?? string.Empty)
                    .Contains(doctorId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        static bool IsConfirmed(Booking b) =>
            string.Equals(b.Status, BookingStatus.Confirmed, StringComparison.OrdinalIgnoreCase);

        static bool IsCompleted(Booking b) =>
            string.Equals(b.Status, BookingStatus.Completed, StringComparison.OrdinalIgnoreCase);

        static bool IsPaid(Booking b) =>
            string.Equals(b.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase);

        var confirmed = items.Where(IsConfirmed).ToList();
        var completed = items.Where(IsCompleted).ToList();
        var paid = items.Where(IsPaid).ToList();
        var nonCancelled = items.Where(x => !string.Equals(x.Status, BookingStatus.Cancelled, StringComparison.OrdinalIgnoreCase)).ToList();

        var estimatedRevenue = confirmed.Sum(x => x.BookedPrice);
        var realizedRevenue = completed.Sum(x => x.BookedPrice);
        var paidRevenue = paid.Sum(x => x.BookedPrice);
        var averageTicket = nonCancelled.Count == 0 ? 0m : nonCancelled.Average(x => x.BookedPrice);

        var byDoctor = items
            .GroupBy(b => b.Slot.DoctorId)
            .Select(g => new DoctorEconomicsDto(
                g.Key,
                g.Count(IsConfirmed),
                g.Count(IsCompleted),
                g.Count(IsPaid),
                g.Where(IsConfirmed).Sum(x => x.BookedPrice),
                g.Where(IsCompleted).Sum(x => x.BookedPrice),
                g.Where(IsPaid).Sum(x => x.BookedPrice)
            ))
            .OrderByDescending(x => x.PaidRevenue)
            .ThenByDescending(x => x.RealizedRevenue)
            .Take(10)
            .ToList();

        var byPrestazione = items
            .GroupBy(b => new
            {
                b.Slot.PrestazioneId,
                PrestazioneName = b.Slot.Prestazione?.Name ?? "Prestazione non associata"
            })
            .Select(g => new PrestazioneEconomicsDto(
                g.Key.PrestazioneId,
                g.Key.PrestazioneName,
                g.Count(IsConfirmed),
                g.Count(IsCompleted),
                g.Count(IsPaid),
                g.Where(IsConfirmed).Sum(x => x.BookedPrice),
                g.Where(IsCompleted).Sum(x => x.BookedPrice),
                g.Where(IsPaid).Sum(x => x.BookedPrice)
            ))
            .OrderByDescending(x => x.PaidRevenue)
            .ThenByDescending(x => x.RealizedRevenue)
            .Take(10)
            .ToList();

        var revenueTrend = items
            .GroupBy(x => x.Slot.StartsAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueTrendPointDto(
                g.Key.ToString("dd/MM"),
                g.Where(IsCompleted).Sum(x => x.BookedPrice),
                g.Where(IsPaid).Sum(x => x.BookedPrice)
            ))
            .ToList();

        return Ok(new DashboardEconomicsDto(
            estimatedRevenue,
            realizedRevenue,
            paidRevenue,
            averageTicket,
            confirmed.Count,
            completed.Count,
            paid.Count,
            byDoctor,
            byPrestazione,
            revenueTrend
        ));
    }
}