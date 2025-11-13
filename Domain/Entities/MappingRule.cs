namespace EventIngestion.Api.Domain.Entities;

public class MappingRule
{
    public int Id { get; set; }
    public string ExternalName { get; set; } = default!;
    public string InternalName { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
}
