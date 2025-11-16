namespace EventIngestion.Api.Domain.Entities;

public class MappedEvent
{
    public long Id { get; set; }
    public long RawEventId { get; set; }
    public RawEvent RawEvent { get; set; }

    public string ActorId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string EventType { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Payload { get; set; }
    public int PublishStatus { get; set; }
    public string FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
