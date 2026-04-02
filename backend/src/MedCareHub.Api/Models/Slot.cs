namespace MedCareHub.Api.Models;

/// <summary>
/// Represents a published time slot for a doctor.
/// </summary>
/// <remarks>
/// Slots can optionally reference a service from the catalog
/// in order to expose duration and pricing information to clients.
/// </remarks>
public sealed class Slot
{
    /// <summary>
    /// Slot identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable doctor identifier, typically mapped from the identity provider.
    /// </summary>
    public string DoctorId { get; set; } = default!;

    /// <summary>
    /// Optional identifier of the linked medical service.
    /// </summary>
    public Guid? PrestazioneId { get; set; }

    /// <summary>
    /// Navigation property to the linked medical service.
    /// </summary>
    public Prestazione? Prestazione { get; set; }

    /// <summary>
    /// Slot start timestamp.
    /// </summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>
    /// Slot end timestamp.
    /// </summary>
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>
    /// Current slot status.
    /// </summary>
    public string Status { get; set; } = SlotStatus.Available;

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}