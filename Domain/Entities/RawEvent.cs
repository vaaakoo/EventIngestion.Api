namespace EventIngestion.Api.Domain.Entities;

public class RawEvent
{
    public long Id { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string RawPayload { get; set; } 
    public int Status { get; set; } 
    public string ErrorMessage { get; set; }
}
