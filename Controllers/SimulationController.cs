using EventIngestion.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace EventIngestion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly IEventIngestionService _ingestionService;
    private readonly Random _random = new();

    public SimulationController(IEventIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Publishes a single predefined sample event.
    /// </summary>
    /// <response code="200">Event published.</response>
    [HttpPost("one")]
    public async Task<IActionResult> PublishOne(CancellationToken ct)
    {
        var sample = new JObject
        {
            ["usr"] = "player_123",
            ["amt"] = "25.50",
            ["curr"] = "GEL",
            ["ts"] = DateTime.UtcNow.ToString("O"),
            ["etype"] = "BetPlaced"
        };

        var (success, error) = await _ingestionService.IngestExternalEventAsync(sample, ct);
        if (!success)
            return BadRequest(new { Success = false, Error = error });

        return Ok(new { Success = true });
    }

    /// <summary>
    /// Generates and publishes 100 random events.
    /// </summary>
    /// <remarks>
    /// Useful for load testing and failure simulation.
    /// </remarks>
    /// <response code="200">Batch finished with stats.</response>
    [HttpPost("batch")]
    public async Task<IActionResult> PublishBatch(CancellationToken ct)
    {
        int total = 100;
        int successCount = 0;
        int failCount = 0;
        var errors = new List<string>();

        for (int i = 0; i < total; i++)
        {
            var playerId = $"player_{_random.Next(1, 1000)}";
            var amount = (_random.NextDouble() * 100).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var currencies = new[] { "GEL", "USD", "EUR" };
            var curr = currencies[_random.Next(currencies.Length)];
            var types = new[] { "BetPlaced", "Deposit", "Withdrawal" };
            var etype = types[_random.Next(types.Length)];

            var sample = new JObject
            {
                ["usr"] = playerId,
                ["amt"] = amount,
                ["curr"] = curr,
                ["ts"] = DateTime.UtcNow.AddMinutes(-_random.Next(0, 1000)).ToString("O"),
                ["etype"] = etype
            };

            var (success, error) = await _ingestionService.IngestExternalEventAsync(sample, ct);
            if (success)
                successCount++;
            else
            {
                failCount++;
                if (error != null)
                    errors.Add(error);
            }
        }

        return Ok(new
        {
            Success = true,
            Total = total,
            SuccessCount = successCount,
            FailCount = failCount,
            Errors = errors.Distinct().ToList()
        });
    }
}
