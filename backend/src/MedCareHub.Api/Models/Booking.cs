namespace MedCareHub.Api.Models;

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string PatientSub { get; set; } = default!;

    public Guid SlotId { get; set; }
    public Slot Slot { get; set; } = default!;

    public string Status { get; set; } = BookingStatus.Confirmed;

    public decimal BookedPrice { get; set; } = 0m;

    public string PaymentStatus { get; set; } = PaymentStatuses.Unpaid;
    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}