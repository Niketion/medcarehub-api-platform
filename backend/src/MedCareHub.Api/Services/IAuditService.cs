namespace MedCareHub.Api.Services;

public interface IAuditService
{
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