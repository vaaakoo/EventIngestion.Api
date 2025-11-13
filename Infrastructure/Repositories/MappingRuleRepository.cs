using EventIngestion.Api.Domain.Entities;
using EventIngestion.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventIngestion.Api.Infrastructure.Repositories;

public class MappingRuleRepository : IMappingRuleRepository
{
    private readonly AppDbContext _db;

    public MappingRuleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MappingRule>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.MappingRules.AsNoTracking().ToListAsync(ct);
    }

    public async Task<MappingRule?> GetByExternalNameAsync(string externalName, CancellationToken ct = default)
    {
        return await _db.MappingRules
            .FirstOrDefaultAsync(x => x.ExternalName == externalName, ct);
    }

    public async Task AddOrUpdateAsync(string externalName, string internalName, CancellationToken ct = default)
    {
        var rule = await _db.MappingRules.FirstOrDefaultAsync(x => x.ExternalName == externalName, ct);
        if (rule == null)
        {
            rule = new MappingRule
            {
                ExternalName = externalName,
                InternalName = internalName,
                UpdatedAt = DateTime.UtcNow
            };
            _db.MappingRules.Add(rule);
        }
        else
        {
            rule.InternalName = internalName;
            rule.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string externalName, CancellationToken ct = default)
    {
        var rule = await _db.MappingRules.FirstOrDefaultAsync(x => x.ExternalName == externalName, ct);
        if (rule != null)
        {
            _db.MappingRules.Remove(rule);
            await _db.SaveChangesAsync(ct);
        }
    }
}
