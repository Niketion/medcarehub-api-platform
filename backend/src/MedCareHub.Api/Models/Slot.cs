namespace MedCareHub.Api.Models;

public sealed class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // doctor "sub" from Keycloak, or any stable identifier
    public string DoctorId { get; set; } = default!;

    public Guid? PrestazioneId { get; set; }
    public Prestazione? Prestazione { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public string Status { get; set; } = SlotStatus.Available;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
