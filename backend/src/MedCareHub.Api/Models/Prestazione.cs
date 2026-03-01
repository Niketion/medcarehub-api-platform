namespace MedCareHub.Api.Models;

public sealed class Prestazione
{
   public Guid Id { get; set; } = Guid.NewGuid();

   public string Name { get; set; } = default!;
   public int? DurationMinutes { get; set; }
   public string? Description { get; set; }

   public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

