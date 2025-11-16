using EventIngestion.Api.Domain.Models;

namespace EventIngestion.Api.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(InternalEvent internalEvent, CancellationToken ct);
}
