using MedCareHub.Api.Services;

namespace MedCareHub.Api.Tests.TestHelpers;

public sealed record AuditCall(
    string Event,
    string ActorSub,
    string? ActorRole,
    string Outcome,
    string ResourceType,
    string ResourceId,
    object? Metadata);

public sealed class FakeAuditService : IAuditService
{
    public List<AuditCall> Calls { get; } = [];

    public Task LogAsync(
        string @event,
        string actorSub,
        string? actorRole,
        string outcome,
        string resourceType,
        string resourceId,
        object? metadata,
        CancellationToken ct)
    {
        Calls.Add(new AuditCall(@event, actorSub, actorRole, outcome, resourceType, resourceId, metadata));
        return Task.CompletedTask;
    }
}