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

        var confirmed = items.Where(IsConfirmed).ToList();
        var completed = items.Where(IsCompleted).ToList();
        var economicItems = items.Where(x => IsConfirmed(x) || IsCompleted(x)).ToList();

        var estimatedRevenue = confirmed.Sum(x => x.BookedPrice);
        var realizedRevenue = completed.Sum(x => x.BookedPrice);
        var averageTicket = economicItems.Count == 0 ? 0m : economicItems.Average(x => x.BookedPrice);

        var byDoctor = items
            .GroupBy(b => b.Slot.DoctorId)
            .Select(g => new DoctorEconomicsDto(
                g.Key,
                g.Count(IsConfirmed),
                g.Count(IsCompleted),
                g.Where(IsConfirmed).Sum(x => x.BookedPrice),
                g.Where(IsCompleted).Sum(x => x.BookedPrice)
            ))
            .OrderByDescending(x => x.RealizedRevenue)
            .ThenByDescending(x => x.EstimatedRevenue)
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
                g.Where(IsConfirmed).Sum(x => x.BookedPrice),
                g.Where(IsCompleted).Sum(x => x.BookedPrice)
            ))
            .OrderByDescending(x => x.RealizedRevenue)
            .ThenByDescending(x => x.EstimatedRevenue)
            .Take(10)
            .ToList();

        return Ok(new DashboardEconomicsDto(
            estimatedRevenue,
            realizedRevenue,
            averageTicket,
            confirmed.Count,
            completed.Count,
            byDoctor,
            byPrestazione
        ));
    }
}