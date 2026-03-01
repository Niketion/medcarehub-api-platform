namespace MedCareHub.Api.Models;

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Keycloak subject (sub) of the patient
    public string PatientSub { get; set; } = default!;

    public Guid SlotId { get; set; }
    public Slot Slot { get; set; } = default!;

    public string Status { get; set; } = BookingStatus.Confirmed;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
