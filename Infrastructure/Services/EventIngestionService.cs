using System.Globalization;
using EventIngestion.Api.Domain.Entities;
using EventIngestion.Api.Domain.Interfaces;
using EventIngestion.Api.Domain.Models;
using EventIngestion.Api.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public async Task<(bool Success, string? Error)> IngestExternalEventAsync(JObject externalJson, CancellationToken ct = default)
    {
        // Step 1 — Save RAW event
        var raw = await SaveRawEventAsync(externalJson, ct);

        try
        {
            // Step 2 — Load mapping rules
            var ruleDict = await LoadMappingDictionaryAsync(ct);

            // Step 3 — Convert external -> internal model
            var mapped = MapExternalToInternalDictionary(externalJson, ruleDict);

            // Step 4 — Validate + convert required fields
            var internalEvent = BuildInternalEvent(mapped);

            // Step 5 — Save mapped event
            var mappedEvent = await SaveMappedEventAsync(raw.Id, internalEvent, ct);

            // Step 6 — Try publish to RabbitMQ
            await TryPublishAsync(internalEvent, raw, mappedEvent, ct);

            return (true, null);
        }
        catch (Exception ex)
        {
            raw.Status = 2;
            raw.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync(ct);

            return (false, ex.Message);
        }
    }

    // -------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------

    private async Task<RawEvent> SaveRawEventAsync(JObject json, CancellationToken ct)
    {
        var raw = new RawEvent
        {
            ReceivedAt = DateTime.UtcNow,
            RawPayload = json.ToString(Formatting.None),
            Status = 0
        };

        _db.RawEvents.Add(raw);
        await _db.SaveChangesAsync(ct);
        return raw;
    }

    private async Task<Dictionary<string, string>> LoadMappingDictionaryAsync(CancellationToken ct)
    {
        var rules = await _mappingRepo.GetAllAsync(ct);
        return rules.ToDictionary(r => r.ExternalName, r => r.InternalName, StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, object?> MapExternalToInternalDictionary(JObject externalJson, Dictionary<string, string> map)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in externalJson.Properties())
        {
            var external = prop.Name;
            var value = prop.Value?.ToObject<object?>();

            if (map.TryGetValue(external, out var internalName))
                result[internalName] = value;
            else
                result[Capitalize(external)] = value;
        }

        return result;
    }

    private InternalEvent BuildInternalEvent(Dictionary<string, object?> mapped)
    {
        // Required: ActorId
        var actorId = ReadRequired(mapped, "ActorId");

        // Required: Amount
        var amountStr = ReadRequired(mapped, "Amount");
        if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            throw new Exception("Amount is not a valid decimal");

        // Required: OccurredAt
        var occurredStr = ReadRequired(mapped, "OccurredAt");
        if (!DateTime.TryParse(occurredStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var occurred))
            throw new Exception("OccurredAt is not a valid datetime");

        // Optional fields
        var currency = mapped.TryGetValue("Currency", out var currObj)
            ? currObj?.ToString() ?? "GEL"
            : "GEL";

        var eventType = mapped.TryGetValue("EventType", out var typeObj)
            ? typeObj?.ToString()
            : null;

        var internalEvent = new InternalEvent
        {
            ActorId = actorId,
            Amount = amount,
            Currency = currency,
            OccurredAt = occurred,
            EventType = eventType
        };

        // Add remaining fields as ExtraFields
        foreach (var kv in mapped)
        {
            if (IsStandardField(kv.Key)) continue;
            internalEvent.ExtraFields[kv.Key] = kv.Value;
        }

        return internalEvent;
    }

    private async Task<MappedEvent> SaveMappedEventAsync(long rawId, InternalEvent model, CancellationToken ct)
    {
        var mapped = new MappedEvent
        {
            RawEventId = rawId,
            ActorId = model.ActorId,
            Amount = model.Amount,
            Currency = model.Currency,
            EventType = model.EventType,
            OccurredAt = model.OccurredAt,
            Payload = JsonConvert.SerializeObject(model),
            PublishStatus = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.MappedEvents.Add(mapped);
        await _db.SaveChangesAsync(ct);

        return mapped;
    }

    private async Task TryPublishAsync(InternalEvent evt, RawEvent raw, MappedEvent mapped, CancellationToken ct)
    {
        try
        {
            await _publisher.PublishAsync(evt, ct);
            mapped.PublishStatus = 1;
            raw.Status = 1;
        }
        catch (Exception ex)
        {
            mapped.PublishStatus = 2;
            mapped.FailureReason = ex.Message;

            raw.Status = 2;
            raw.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Publishing failed for RawEventId={Id}", raw.Id);
        }

        await _db.SaveChangesAsync(ct);
    }

    // Small helpers
    private string ReadRequired(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var obj) || string.IsNullOrWhiteSpace(obj?.ToString()))
            throw new Exception($"Missing required internal field: {key}");

        return obj!.ToString()!;
    }

    private static bool IsStandardField(string key) =>
        key is "ActorId" or "Amount" or "Currency" or "OccurredAt" or "EventType";

    private static string Capitalize(string s) =>
        char.ToUpper(s[0]) + s.Substring(1);
}
