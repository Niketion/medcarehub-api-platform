namespace MedCareHub.Api.Models;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string Event { get; set; } = default!;
    public string ActorSub { get; set; } = default!;
    public string? ActorRole { get; set; }

    public string Outcome { get; set; } = AuditOutcome.Success;

    public string ResourceType { get; set; } = default!;
    public string ResourceId { get; set; } = default!;

    public string? MetadataJson { get; set; }
}
