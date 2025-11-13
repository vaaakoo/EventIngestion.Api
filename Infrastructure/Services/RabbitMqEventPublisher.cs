using System.Text;
using EventIngestion.Api.Domain.Interfaces;
using EventIngestion.Api.Domain.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace EventIngestion.Api.Infrastructure.Services;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly Random _random = new();

    public RabbitMqEventPublisher(
        ILogger<RabbitMqEventPublisher> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task PublishAsync(InternalEvent internalEvent, CancellationToken ct = default)
    {
        // Random failure simulation (e.g. 25%)
        var dice = _random.NextDouble();
        if (dice < 0.25)
        {
            _logger.LogWarning("Simulated publishing failure for event ActorId={ActorId}", internalEvent.ActorId);
            throw new Exception("Simulated publishing failure");
        }

        // რეალური RabbitMQ publish (თუ გინდა რეალურად):
        var rabbitHost = _configuration["RabbitMq:Host"] ?? "localhost";
        var exchangeName = _configuration["RabbitMq:Exchange"] ?? "events.exchange";

        var factory = new ConnectionFactory
        {
            HostName = rabbitHost
        };

        // Simple example (no pooling, for demo only)
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

        var payloadJson = JsonConvert.SerializeObject(internalEvent);
        var body = Encoding.UTF8.GetBytes(payloadJson);

        var routingKey = $"events.{(internalEvent.EventType ?? "generic").ToLower()}";

        channel.BasicPublish(
            exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: null,
            body: body);

        _logger.LogInformation("Published event to RabbitMQ. ActorId={ActorId}, RoutingKey={RoutingKey}",
            internalEvent.ActorId, routingKey);

        await Task.CompletedTask;
    }
}
