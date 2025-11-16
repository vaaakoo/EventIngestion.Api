namespace EventIngestion.Api.Domain.Entities;

public class MappingRule
{
    public int Id { get; set; }
    public string ExternalName { get; set; }
    public string InternalName { get; set; }
    public DateTime UpdatedAt { get; set; }
}
