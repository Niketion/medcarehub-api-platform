using System.Text.Json;
using MedCareHub.Api.Data;
using MedCareHub.Api.Models;

namespace MedCareHub.Api.Services;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(string @event, string actorSub, string? actorRole, string outcome, string resourceType, string resourceId, object? metadata, CancellationToken ct)
    {
        var item = new AuditLog
        {
            Event = @event,
            ActorSub = actorSub,
            ActorRole = actorRole,
            Outcome = string.IsNullOrWhiteSpace(outcome) ? AuditOutcome.Success : outcome,
            ResourceType = resourceType,
            ResourceId = resourceId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata)
        };

        _db.AuditLogs.Add(item);
        await _db.SaveChangesAsync(ct);
    }
}