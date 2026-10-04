using StackDuel.Application.Messaging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace StackDuel.Infrastructure.Messaging;

internal sealed class RabbitMqMessagePublisher(IConnection connection) : IMessagePublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IMessage
    {
        using var channel = connection.CreateModel();
        string queueName = QueueNames.ForType<T>();
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: body);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes via an x-delayed-message exchange (rabbitmq_delayed_message_exchange plugin)
    /// so scheduled delivery works locally the same way it does against Azure Service Bus in
    /// prod. Requires the plugin enabled on the broker — see docker-compose.yml.
    /// </summary>
    public Task<long> PublishScheduledAsync<T>(
        T message,
        DateTimeOffset scheduledEnqueueTimeUtc,
        CancellationToken cancellationToken = default
    )
        where T : class, IMessage
    {
        using var channel = connection.CreateModel();
        string queueName = QueueNames.ForType<T>();
        string exchangeName = $"{queueName}.delayed";

        channel.ExchangeDeclare(
            exchangeName,
            type: "x-delayed-message",
            durable: true,
            arguments: new Dictionary<string, object> { ["x-delayed-type"] = "direct" }
        );

        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queueName, exchangeName, routingKey: queueName);

        long delayMs = Math.Max(0, (long)(scheduledEnqueueTimeUtc - DateTimeOffset.UtcNow).TotalMilliseconds);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object> { ["x-delay"] = delayMs };

        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublish(exchange: exchangeName, routingKey: queueName, basicProperties: props, body: body);

        // RabbitMQ's delayed-message-exchange plugin has no server-generated handle to return
        // (unlike Azure Service Bus's sequence number). 0 is a placeholder; do not rely on it
        // for CancelScheduledAsync — see that method.
        return Task.FromResult(0L);
    }

    /// <summary>
    /// Not supported: the delayed-message-exchange plugin has no cancel-by-handle API — once
    /// published, a delayed message will eventually land in the queue. Consumers must
    /// re-verify state and treat a message for an already-finalized entity as a no-op rather
    /// than depending on cancellation.
    /// </summary>
    public Task CancelScheduledAsync<T>(long sequenceNumber, CancellationToken cancellationToken = default)
        where T : class, IMessage =>
        throw new NotSupportedException(
            "RabbitMQ's delayed-message-exchange plugin does not support cancelling a scheduled "
                + "message by handle. Rely on idempotent, state-checking consumers instead."
        );
}