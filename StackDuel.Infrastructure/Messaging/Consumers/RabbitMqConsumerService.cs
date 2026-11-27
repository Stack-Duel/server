using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackDuel.Application.Configuration;
using System.Text;

namespace StackDuel.Infrastructure.Messaging.Consumers;

internal sealed partial class RabbitMqConsumerService(
    IConnection connection,
    MessageBusOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumerService> logger
) : BackgroundService
{
    private const int MaxProcessingRetries = 5;
    private const string RetryCountHeader = "x-retry-count";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int consumerConcurrency = Math.Max(1, options.RabbitMQ.ConsumerConcurrency);
        var channels = new List<IModel>();

        try
        {
            foreach (MessageSubscription subscription in MessageSubscriptions.All)
                Subscribe(subscription, channels, consumerConcurrency, stoppingToken);
        }
        catch (Exception ex)
        {
            LogStartupFailed(ex);
            CloseChannels(channels);
            return Task.CompletedTask;
        }

        stoppingToken.Register(() => CloseChannels(channels));

        return Task.CompletedTask;
    }

    private static void CloseChannels(IEnumerable<IModel> channels)
    {
        foreach (var channel in channels)
        {
            try
            {
                channel.Close();
            }
            catch { }
            channel.Dispose();
        }
    }

    private void Subscribe(
        MessageSubscription subscription,
        ICollection<IModel> channels,
        int consumerConcurrency,
        CancellationToken stoppingToken
    )
    {
        string queueName = subscription.QueueName;
        string retryExchangeName = $"{queueName}.retry";
        string deadLetterQueueName = $"{queueName}.dlq";

        for (int i = 0; i < consumerConcurrency; i++)
        {
            IModel channel = connection.CreateModel();
            channels.Add(channel);

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            if (subscription.UsesDelayedExchange)
                DeclareDelayedDeliveryInfrastructure(channel, queueName);

            DeclareRetryInfrastructure(channel, queueName, retryExchangeName, deadLetterQueueName);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            AsyncEventingBasicConsumer consumer = new(channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    object? message = subscription.Deserialize(ea.Body);
                    if (message is not null)
                    {
                        LogMessageReceived(subscription.MessageType.Name, message.ToString() ?? string.Empty);
                        using var scope = scopeFactory.CreateScope();
                        await subscription.HandleAsync(scope.ServiceProvider, message, stoppingToken);
                    }

                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    LogProcessingError(ex);
                    HandleProcessingFailure(channel, ea, queueName, retryExchangeName, deadLetterQueueName);
                }
            };

            channel.BasicConsume(queueName, autoAck: false, consumer);
        }
    }

    /// <summary>
    /// Declares the exchange that scheduled publishes target, and binds the queue to it.
    /// Declared defensively in case this consumer starts before anything has published yet.
    /// </summary>
    private static void DeclareDelayedDeliveryInfrastructure(IModel channel, string queueName)
    {
        string exchangeName = $"{queueName}.delayed";

        channel.ExchangeDeclare(
            exchangeName,
            type: "x-delayed-message",
            durable: true,
            arguments: new Dictionary<string, object> { ["x-delayed-type"] = "direct" }
        );
        channel.QueueBind(queueName, exchangeName, routingKey: queueName);
    }

    private static void DeclareRetryInfrastructure(
        IModel channel,
        string queueName,
        string retryExchangeName,
        string deadLetterQueueName
    )
    {
        channel.ExchangeDeclare(
            retryExchangeName,
            type: "x-delayed-message",
            durable: true,
            arguments: new Dictionary<string, object> { ["x-delayed-type"] = "direct" }
        );
        channel.QueueBind(queueName, retryExchangeName, routingKey: queueName);
        channel.QueueDeclare(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
    }

    private void HandleProcessingFailure(
        IModel channel,
        BasicDeliverEventArgs ea,
        string queueName,
        string retryExchangeName,
        string deadLetterQueueName
    )
    {
        int retryCount = GetRetryCount(ea.BasicProperties);

        if (retryCount >= MaxProcessingRetries)
        {
            LogMovedToDeadLetter(queueName, retryCount);

            IBasicProperties deadLetterProps = channel.CreateBasicProperties();
            deadLetterProps.Headers = new Dictionary<string, object> { [RetryCountHeader] = retryCount };
            channel.BasicPublish(string.Empty, deadLetterQueueName, deadLetterProps, ea.Body);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        TimeSpan delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retryCount)));
        LogRetryScheduled(queueName, retryCount + 1, delay);

        IBasicProperties retryProps = channel.CreateBasicProperties();
        retryProps.Headers = new Dictionary<string, object>
        {
            [RetryCountHeader] = retryCount + 1,
            ["x-delay"] = (int)delay.TotalMilliseconds,
        };
        channel.BasicPublish(retryExchangeName, queueName, retryProps, ea.Body);
        channel.BasicAck(ea.DeliveryTag, multiple: false);
    }

    private static int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(RetryCountHeader, out object? value))
            return 0;

        return value switch
        {
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out int parsed) => parsed,
            _ => 0,
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Received {MessageType}: {Message}")]
    private partial void LogMessageReceived(string messageType, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message from RabbitMQ")]
    private partial void LogProcessingError(Exception ex);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Failed to start RabbitMQ consumers")]
    private partial void LogStartupFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Retrying message from {QueueName} (attempt {RetryCount}) after {Delay}"
    )]
    private partial void LogRetryScheduled(string queueName, int retryCount, TimeSpan delay);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Message from {QueueName} failed {RetryCount} times and was moved to its dead-letter queue"
    )]
    private partial void LogMovedToDeadLetter(string queueName, int retryCount);
}