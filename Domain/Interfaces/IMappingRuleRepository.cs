using EventIngestion.Api.Domain.Entities;

namespace EventIngestion.Api.Domain.Interfaces;

public interface IMappingRuleRepository
{
    Task<List<MappingRule>> GetAllAsync(CancellationToken ct = default);
    Task<MappingRule?> GetByExternalNameAsync(string externalName, CancellationToken ct = default);
    Task AddOrUpdateAsync(string externalName, string internalName, CancellationToken ct = default);
    Task RemoveAsync(string externalName, CancellationToken ct = default);
}
