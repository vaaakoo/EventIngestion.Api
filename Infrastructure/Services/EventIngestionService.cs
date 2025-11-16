using System.Globalization;
using EventIngestion.Api.Domain.Entities;
using EventIngestion.Api.Domain.Interfaces;
using EventIngestion.Api.Domain.Models;
using EventIngestion.Api.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventIngestion.Api.Application.Services;

public class EventIngestionService : IEventIngestionService
{
    private readonly AppDbContext _db;
    private readonly IMappingRuleRepository _mappingRepo;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<EventIngestionService> _logger;

    public EventIngestionService(
        AppDbContext db,
        IMappingRuleRepository mappingRepo,
        IEventPublisher publisher,
        ILogger<EventIngestionService> logger)
    {
        _db = db;
        _mappingRepo = mappingRepo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> IngestExternalEventAsync(
        JObject externalJson,
        CancellationToken ct = default)
    {
        // 1) შევინახოთ RawEvent
        var raw = new RawEvent
        {
            ReceivedAt = DateTime.UtcNow,
            RawPayload = externalJson.ToString(Formatting.None),
            Status = 0
        };

        _db.RawEvents.Add(raw);
        await _db.SaveChangesAsync(ct);

        try
        {
            // 2) წავიღოთ mapping წესები
            var rules = await _mappingRepo.GetAllAsync(ct);
            var ruleDict = rules.ToDictionary(r => r.ExternalName, r => r.InternalName, StringComparer.OrdinalIgnoreCase);

            // 3) გადავიყვანოთ external JObject შიდა dictionary-ში internal სახელებით
            var mappedDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in externalJson.Properties())
            {
                var externalName = prop.Name;
                var value = prop.Value?.ToObject<object?>();

                if (ruleDict.TryGetValue(externalName, out var internalName))
                {
                    mappedDict[internalName] = value;
                }
                else
                {
                    // default mapping: externalName 그대로 Internal-ს ვტოვებთ (PascalCase little)
                    var internalDefaultName = char.ToUpper(externalName[0]) + externalName.Substring(1);
                    mappedDict[internalDefaultName] = value;
                }
            }

            // 4) Validation – აუცილებელი ველები
            if (!mappedDict.TryGetValue("ActorId", out var actorObj) || actorObj is null || string.IsNullOrWhiteSpace(actorObj.ToString()))
            {
                throw new Exception("Missing required internal field: ActorId");
            }

            if (!mappedDict.TryGetValue("Amount", out var amountObj) || amountObj is null)
            {
                throw new Exception("Missing required internal field: Amount");
            }

            if (!mappedDict.TryGetValue("OccurredAt", out var occuredObj) || occuredObj is null)
            {
                throw new Exception("Missing required internal field: OccurredAt");
            }

            // 5) Convert types
            var actorId = actorObj.ToString()!;
            if (!decimal.TryParse(amountObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                throw new Exception("Amount is not a valid decimal");

            if (!DateTime.TryParse(occuredObj.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var occurredAt))
                throw new Exception("OccurredAt is not a valid datetime");

            var currency = mappedDict.TryGetValue("Currency", out var currencyObj)
                ? currencyObj?.ToString() ?? "GEL"
                : "GEL";

            var eventType = mappedDict.TryGetValue("EventType", out var typeObj)
                ? typeObj?.ToString()
                : null;

            // 6) InternalEvent (CQRS-სთვის)
            var internalEvent = new InternalEvent
            {
                ActorId = actorId,
                Amount = amount,
                Currency = currency,
                OccurredAt = occurredAt,
                EventType = eventType
            };

            // Extra fields
            foreach (var kv in mappedDict)
            {
                if (kv.Key is "ActorId" or "Amount" or "Currency" or "OccurredAt" or "EventType")
                    continue;

                internalEvent.ExtraFields[kv.Key] = kv.Value;
            }

            // 7) შევინახოთ MappedEvent DB-ში
            var mappedEvent = new MappedEvent
            {
                RawEventId = raw.Id,
                ActorId = internalEvent.ActorId,
                Amount = internalEvent.Amount,
                Currency = internalEvent.Currency,
                EventType = internalEvent.EventType,
                OccurredAt = internalEvent.OccurredAt,
                Payload = JsonConvert.SerializeObject(internalEvent),
                PublishStatus = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.MappedEvents.Add(mappedEvent);
            await _db.SaveChangesAsync(ct);

            // 8) Publish to RabbitMQ (with random failure simulation)
            try
            {
                await _publisher.PublishAsync(internalEvent, ct);
                mappedEvent.PublishStatus = 1;
                mappedEvent.FailureReason = null;
                raw.Status = 1;
                raw.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publishing failed for RawEventId={RawId}", raw.Id);
                mappedEvent.PublishStatus = 2;
                mappedEvent.FailureReason = ex.Message;
                raw.Status = 2;
                raw.ErrorMessage = ex.Message;
            }

            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest external event. RawEventId={RawId}", raw.Id);
            raw.Status = 2;
            raw.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync(ct);
            return (false, ex.Message);
        }
    }
}
