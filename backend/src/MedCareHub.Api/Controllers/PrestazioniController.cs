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
[Route("api/prestazioni")]
public sealed class PrestazioniController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public PrestazioniController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PrestazioneDto>>> Get(CancellationToken ct)
    {
        var items = await _db.Prestazioni.AsNoTracking()
            .OrderBy(p => p.Name)
            .Take(500)
            .ToListAsync(ct);

        return Ok(items.Select(p => new PrestazioneDto(
            p.Id,
            p.Name,
            p.DurationMinutes,
            p.Description,
            p.BasePrice,
            p.CreatedAt
        )));
    }

    [HttpPost]
    [Authorize(Policy = Policies.Staff)]
    public async Task<ActionResult<PrestazioneDto>> Create([FromBody] CreatePrestazioneRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required" });

        if (req.BasePrice < 0)
            return BadRequest(new { error = "BasePrice must be >= 0" });

        var p = new Prestazione
        {
            Name = req.Name.Trim(),
            DurationMinutes = req.DurationMinutes,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            BasePrice = req.BasePrice
        };

        _db.Prestazioni.Add(p);
        await _db.SaveChangesAsync(ct);

        var actorSub = User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";
        var actorRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        await _audit.LogAsync(
            "prestazione_created",
            actorSub,
            actorRole,
            AuditOutcome.Success,
            "prestazione",
            p.Id.ToString(),
            new { p.Name, p.DurationMinutes, p.BasePrice },
            ct);

        return Ok(new PrestazioneDto(
            p.Id,
            p.Name,
            p.DurationMinutes,
            p.Description,
            p.BasePrice,
            p.CreatedAt
        ));
    }
}