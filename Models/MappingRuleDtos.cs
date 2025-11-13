namespace EventIngestion.Api.Models;

public class MappingRuleDto
{
    public string ExternalName { get; set; } = default!;
    public string InternalName { get; set; } = default!;
}
