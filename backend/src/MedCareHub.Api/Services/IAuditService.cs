namespace MedCareHub.Api.Services;

/// <summary>
/// Persists audit events related to security-sensitive or business-relevant operations.
/// </summary>
/// <remarks>
/// Audit records are used to provide traceability over actions such as:
/// slot publication, bookings, payments, and report upload/download.
/// </remarks>
public interface IAuditService
{
    /// <summary>
    /// Writes an audit log entry.
    /// </summary>
    /// <param name="event">Machine-readable event name.</param>
    /// <param name="actorSub">Identifier of the authenticated user who triggered the event.</param>
    /// <param name="actorRole">Role used to perform the operation, if available.</param>
    /// <param name="outcome">Logical result of the action, for example success or fail.</param>
    /// <param name="resourceType">Type of resource involved in the operation.</param>
    /// <param name="resourceId">Identifier of the resource involved in the operation.</param>
    /// <param name="metadata">Optional structured payload serialized as JSON.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>A task that completes when the log entry has been persisted.</returns>
    Task LogAsync(
        string @event,
        string actorSub,
        string? actorRole,
        string outcome,
        string resourceType,
        string resourceId,
        object? metadata,
        CancellationToken ct);
}