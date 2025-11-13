namespace EventIngestion.Api.Domain.Models;

public class InternalEvent
{
    public string ActorId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public string? EventType { get; set; }
    public Dictionary<string, object?> ExtraFields { get; set; } = new();
}
