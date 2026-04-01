using System.Security.Claims;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Data;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Models;
using MedCareHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Controllers;

[ApiController]
[Route("api/slots")]
public sealed class SlotsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public SlotsController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<SlotDto>>> Get(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? doctorId,
        CancellationToken ct)
    {
        var q = _db.Slots.AsNoTracking()
            .Include(s => s.Prestazione)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(doctorId))
            q = q.Where(s => s.DoctorId == doctorId);

        if (from.HasValue)
            q = q.Where(s => s.StartsAt >= from.Value);

        if (to.HasValue)
            q = q.Where(s => s.EndsAt <= to.Value);

        var items = await q.OrderBy(s => s.StartsAt).Take(500).ToListAsync(ct);

        return Ok(items.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Policy = Policies.Staff)]
    public async Task<ActionResult<SlotDto>> Create([FromBody] CreateSlotRequest req, CancellationToken ct)
    {
        if (req.EndsAt <= req.StartsAt)
            return BadRequest(new { error = "EndsAt must be after StartsAt" });

        Prestazione? prestazione = null;

        if (req.PrestazioneId.HasValue)
        {
            prestazione = await _db.Prestazioni.FirstOrDefaultAsync(p => p.Id == req.PrestazioneId.Value, ct);
            if (prestazione is null)
                return BadRequest(new { error = "PrestazioneId not found" });
        }

        var slot = new Slot
        {
            DoctorId = req.DoctorId,
            PrestazioneId = req.PrestazioneId,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Status = SlotStatus.Available
        };

        _db.Slots.Add(slot);
        await _db.SaveChangesAsync(ct);

        var actorSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        await _audit.LogAsync(
            "slot_created",
            actorSub,
            actorRole,
            AuditOutcome.Success,
            "slot",
            slot.Id.ToString(),
            new { slot.DoctorId, slot.StartsAt, slot.EndsAt, slot.PrestazioneId },
            ct);

        slot.Prestazione = prestazione;

        return CreatedAtAction(nameof(Get), new { id = slot.Id }, ToDto(slot));
    }

    private static SlotDto ToDto(Slot s) => new(
        s.Id,
        s.DoctorId,
        s.PrestazioneId,
        s.Prestazione?.Name,
        s.Prestazione?.BasePrice,
        s.StartsAt,
        s.EndsAt,
        s.Status
    );
}