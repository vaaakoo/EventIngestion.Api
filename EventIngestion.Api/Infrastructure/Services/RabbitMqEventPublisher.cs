using System.Text;
using EventIngestion.Api.Domain.Interfaces;
using EventIngestion.Api.Domain.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace EventIngestion.Api.Infrastructure.Services;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    // Cached configuration values
    private readonly string _host;
    private readonly string _exchange;

    private readonly Random _random = new();

    public RabbitMqEventPublisher(
        ILogger<RabbitMqEventPublisher> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        _host = configuration["RabbitMq:Host"] ?? "localhost";
        _exchange = configuration["RabbitMq:Exchange"] ?? "events.exchange";
    }

    public async Task PublishAsync(InternalEvent internalEvent, CancellationToken ct)
    {
        // 1) Simulated failure (25%)
        if (_random.NextDouble() < 0.25)
        {
            _logger.LogWarning("Simulated RabbitMQ failure. ActorId={ActorId}, EventType={EventType}", internalEvent.ActorId, internalEvent.EventType);

            throw new Exception("Simulated publishing failure");
        }

        // 2) Real RabbitMQ publish
        var factory = new ConnectionFactory
        {
            HostName = _host
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Topic, durable: true);

        var payloadJson = JsonConvert.SerializeObject(internalEvent);
        var body = Encoding.UTF8.GetBytes(payloadJson);

        var routingKey = $"events.{(internalEvent.EventType ?? "generic").ToLower()}";

        channel.BasicPublish(exchange: _exchange, routingKey: routingKey, basicProperties: null, body: body);

        _logger.LogInformation("Published to RabbitMQ. ActorId={ActorId}, EventType={EventType}, RoutingKey={RoutingKey}", internalEvent.ActorId, internalEvent.EventType, routingKey);

        await Task.CompletedTask;
    }
}
