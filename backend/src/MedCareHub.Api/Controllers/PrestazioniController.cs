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

/// <summary>
/// Exposes endpoints for the catalog of medical services.
/// </summary>
/// <remarks>
/// The service catalog is used to associate duration and base price
/// with doctor slots and downstream bookings.
/// </remarks>
[ApiController]
[Route("api/prestazioni")]
public sealed class PrestazioniController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    /// <summary>
    /// Creates a new instance of <see cref="PrestazioniController"/>.
    /// </summary>
    public PrestazioniController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    /// <summary>
    /// Returns the list of available medical services.
    /// </summary>
    /// <response code="200">Services returned successfully.</response>
    /// <response code="401">Authentication is required.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PrestazioneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Creates a new medical service in the catalog.
    /// </summary>
    /// <response code="200">Service created successfully.</response>
    /// <response code="400">The request payload is invalid.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user is not allowed to create services.</response>
    [HttpPost]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(PrestazioneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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