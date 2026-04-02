namespace MedCareHub.Api.Models;

/// <summary>
/// Represents a patient booking associated with a published slot.
/// </summary>
/// <remarks>
/// Economic values are copied at booking time so that later catalog updates
/// do not alter the historical value of an existing booking.
/// </remarks>
public sealed class Booking
{
    /// <summary>
    /// Booking identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable identifier of the patient who created the booking.
    /// </summary>
    public string PatientSub { get; set; } = default!;

    /// <summary>
    /// Identifier of the booked slot.
    /// </summary>
    public Guid SlotId { get; set; }

    /// <summary>
    /// Navigation property to the booked slot.
    /// </summary>
    public Slot Slot { get; set; } = default!;

    /// <summary>
    /// Current booking lifecycle status.
    /// </summary>
    public string Status { get; set; } = BookingStatus.Confirmed;

    /// <summary>
    /// Economic amount captured at booking time.
    /// </summary>
    public decimal BookedPrice { get; set; } = 0m;

    /// <summary>
    /// Payment status used by operational and dashboard flows.
    /// </summary>
    public string PaymentStatus { get; set; } = PaymentStatuses.Unpaid;

    /// <summary>
    /// Timestamp set when the booking is marked as paid.
    /// </summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}