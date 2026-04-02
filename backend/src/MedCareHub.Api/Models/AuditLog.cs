namespace MedCareHub.Api.Models;

/// <summary>
/// Represents a persisted audit event for traceability purposes.
/// </summary>
/// <remarks>
/// Audit records capture who performed an action, on which resource,
/// with which outcome, and with optional structured metadata.
/// </remarks>
public sealed class AuditLog
{
    /// <summary>
    /// Audit record identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp of the audited event.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Machine-readable event name.
    /// </summary>
    public string Event { get; set; } = default!;

    /// <summary>
    /// Stable identifier of the authenticated actor.
    /// </summary>
    public string ActorSub { get; set; } = default!;

    /// <summary>
    /// Role used to perform the action, when available.
    /// </summary>
    public string? ActorRole { get; set; }

    /// <summary>
    /// Logical outcome of the operation.
    /// </summary>
    public string Outcome { get; set; } = AuditOutcome.Success;

    /// <summary>
    /// Type of resource involved in the operation.
    /// </summary>
    public string ResourceType { get; set; } = default!;

    /// <summary>
    /// Identifier of the resource involved in the operation.
    /// </summary>
    public string ResourceId { get; set; } = default!;

    /// <summary>
    /// Optional structured metadata serialized as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }
}