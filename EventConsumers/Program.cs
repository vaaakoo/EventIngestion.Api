using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Connection settings
var factory = new ConnectionFactory()
{
    HostName = "localhost",
    DispatchConsumersAsync = true // async consumer support
};

// Create connection once
using var connection = factory.CreateConnection();

// Start all consumers
StartConsumer(connection, "bets.q", "BetPlaced");
StartConsumer(connection, "deposits.q", "Deposit");
StartConsumer(connection, "withdrawals.q", "Withdrawal");

Console.WriteLine("Consumers running. Press ENTER to exit.");
Console.ReadLine();


// Method to start a consumer for a specific queue
static void StartConsumer(IConnection connection, string queueName, string label)
{
    var channel = connection.CreateModel();

    channel.QueueDeclare(
        queue: queueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var consumer = new AsyncEventingBasicConsumer(channel);

    consumer.Received += async (sender, ea) =>
    {
        try
        {
            string json = Encoding.UTF8.GetString(ea.Body.ToArray());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[{label} Consumer] Message from {queueName}:");
            Console.ResetColor();
            Console.WriteLine(json);

            // ack the message
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{label} Consumer] ERROR: {ex.Message}");
            Console.ResetColor();

            // if error, reject message and requeue
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    };

    channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

    Console.WriteLine($"✔ Consumer for '{queueName}' started ({label})");
}
