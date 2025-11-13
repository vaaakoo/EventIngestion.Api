using Newtonsoft.Json.Linq;

namespace EventIngestion.Api.Domain.Interfaces;

public interface IEventIngestionService
{
    Task<(bool Success, string? Error)> IngestExternalEventAsync(
        JObject externalJson,
        string? provider,
        CancellationToken ct = default);
}
