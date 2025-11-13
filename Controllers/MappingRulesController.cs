using EventIngestion.Api.Domain.Interfaces;
using EventIngestion.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventIngestion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MappingRulesController : ControllerBase
{
    private readonly IMappingRuleRepository _repo;

    public MappingRulesController(IMappingRuleRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rules = await _repo.GetAllAsync(ct);
        var result = rules.Select(r => new MappingRuleDto
        {
            ExternalName = r.ExternalName,
            InternalName = r.InternalName
        });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddOrUpdate([FromBody] MappingRuleDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _repo.AddOrUpdateAsync(dto.ExternalName, dto.InternalName, ct);
        return Ok(new { Success = true });
    }

    [HttpDelete("{externalName}")]
    public async Task<IActionResult> Delete(string externalName, CancellationToken ct)
    {
        await _repo.RemoveAsync(externalName, ct);
        return Ok(new { Success = true });
    }
}
