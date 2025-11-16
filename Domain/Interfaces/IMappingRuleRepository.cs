using EventIngestion.Api.Domain.Entities;

namespace EventIngestion.Api.Domain.Interfaces;

public interface IMappingRuleRepository
{
    Task<List<MappingRule>> GetAllAsync(CancellationToken ct);
    Task AddOrUpdateAsync(string externalName, string internalName, CancellationToken ct);
    Task RemoveAsync(string externalName, CancellationToken ct);
}
