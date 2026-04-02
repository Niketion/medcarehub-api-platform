namespace MedCareHub.Api.Models;

/// <summary>
/// Represents a medical service available in the catalog.
/// </summary>
/// <remarks>
/// Services can be linked to slots so that bookings inherit
/// operational and economic information from the catalog.
/// </remarks>
public sealed class Prestazione
{
    /// <summary>
    /// Service identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable service name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Optional expected duration in minutes.
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Optional textual description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base price copied into bookings at booking time.
    /// </summary>
    public decimal BasePrice { get; set; } = 0m;

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}