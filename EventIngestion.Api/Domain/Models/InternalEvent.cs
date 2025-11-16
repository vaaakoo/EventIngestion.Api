namespace EventIngestion.Api.Domain.Models;

public class InternalEvent
{
    public string ActorId { get; set; } 
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, object?> ExtraFields { get; set; } = new();
}
