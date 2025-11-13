namespace EventIngestion.Api.Domain.Entities;

public class MappedEvent
{
    public long Id { get; set; }
    public long RawEventId { get; set; }
    public RawEvent RawEvent { get; set; } = default!;

    public string ActorId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string? EventType { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Payload { get; set; } = default!;
    public int PublishStatus { get; set; } // 0=pending,1=sent,2=failed
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
