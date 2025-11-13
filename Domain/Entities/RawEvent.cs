namespace EventIngestion.Api.Domain.Entities;

public class RawEvent
{
    public long Id { get; set; }
    public string? Provider { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string RawPayload { get; set; } = default!;
    public int Status { get; set; } // 0 = Pending, 1 = Processed, 2 = Failed
    public string? ErrorMessage { get; set; }
}
