using EventIngestion.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace EventIngestion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventIngestionService _ingestionService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventIngestionService ingestionService,
        ILogger<EventsController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Accepts any external JSON event and processes it.
    /// </summary>
    /// <remarks>
    /// Required internal fields after mapping: ActorId, Amount, OccurredAt.<br/>
    /// Provide raw JSON body. Mapping rules are applied automatically.
    /// </remarks>
    /// <response code="200">Event ingested successfully.</response>
    /// <response code="400">Invalid JSON or failed validation.</response>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] object body, CancellationToken ct)
    {
        JObject externalEvent;
        try
        {
            externalEvent = JObject.FromObject(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid JSON");
            return BadRequest(new { Success = false, Error = "Invalid JSON" });
        }

        var (success, error) = await _ingestionService.IngestExternalEventAsync(externalEvent, ct);
        if (!success)
            return BadRequest(new { Success = false, Error = error });

        return Ok(new { Success = true, Message = "Event ingested successfully" });
    }
}
