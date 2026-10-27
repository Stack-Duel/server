using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackDuel.Application.Commands.Games.CompleteExpiredGame;
using StackDuel.Application.Configuration;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Infrastructure.Jobs.ProblemValidation;
using StackDuel.Infrastructure.Jobs.Submissions;
using StackDuel.Infrastructure.Jobs.TestCaseGeneration;
using StackDuel.Infrastructure.Messaging;
using System.Text;
using System.Text.Json;

namespace StackDuel.Infrastructure.Messaging.Consumers;

internal sealed partial class RabbitMqConsumerService(
    IConnection connection,
    MessageBusOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumerService> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int consumerConcurrency = Math.Max(1, options.RabbitMQ.ConsumerConcurrency);
        var channels = new List<IModel>();

        Subscribe<SubmissionCreatedMessage>(
            channels,
            (sp, m, ct) =>
                sp.GetRequiredService<SubmissionJobProcessorService>().RunForSubmissionAsync(m.SubmissionId, ct),
            consumerConcurrency,
            stoppingToken
        );

        Subscribe<SubmissionJobContinuationMessage>(
            channels,
            (sp, m, ct) =>
                sp.GetRequiredService<SubmissionJobProcessorService>().RunForSubmissionAsync(m.SubmissionId, ct),
            consumerConcurrency,
            stoppingToken
        );

        Subscribe<ProblemValidationJobContinuationMessage>(
            channels,
            (sp, m, ct) =>
                sp.GetRequiredService<ProblemValidationJobProcessorService>().RunPendingForJobAsync(m.JobId, ct),
            consumerConcurrency,
            stoppingToken
        );

        Subscribe<TestCaseGenerationJobContinuationMessage>(
            channels,
            (sp, m, ct) => sp.GetRequiredService<TestCaseGenerationJobProcessorService>().RunForJobAsync(m.JobId, ct),
            consumerConcurrency,
            stoppingToken
        );

        Subscribe<TestCaseGenerationJobCompletedMessage>(
            channels,
            (sp, _, ct) =>
                sp.GetRequiredService<ProblemValidationJobProcessorService>().RunAwaitingGenerationPassAsync(ct),
            consumerConcurrency,
            stoppingToken
        );

        SubscribeGameExpiryMessages(channels, consumerConcurrency, stoppingToken);

        stoppingToken.Register(() =>
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
        });

        return Task.CompletedTask;
    }

    private const int MaxProcessingRetries = 5;
    private const string RetryCountHeader = "x-retry-count";

    private void Subscribe<TMessage>(
        ICollection<IModel> channels,
        Func<IServiceProvider, TMessage, CancellationToken, Task> handle,
        int consumerConcurrency,
        CancellationToken stoppingToken
    )
        where TMessage : IMessage
    {
        string queueName = QueueNames.ForType<TMessage>();
        string retryExchangeName = $"{queueName}.retry";
        string deadLetterQueueName = $"{queueName}.dlq";

        for (int i = 0; i < consumerConcurrency; i++)
        {
            IModel channel = connection.CreateModel();
            channels.Add(channel);

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            DeclareRetryInfrastructure(channel, queueName, retryExchangeName, deadLetterQueueName);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            AsyncEventingBasicConsumer consumer = new(channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    string body = Encoding.UTF8.GetString(ea.Body.Span);
                    TMessage? message = JsonSerializer.Deserialize<TMessage>(body);
                    if (message is not null)
                    {
                        LogMessageReceived(typeof(TMessage).Name, message.ToString() ?? string.Empty);
                        using var scope = scopeFactory.CreateScope();
                        await handle(scope.ServiceProvider, message, stoppingToken);
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

    /// <summary>
    /// Consumes from the queue bound to the x-delayed-message exchange. Declares the same
    /// exchange/queue/binding defensively in case this consumer starts before anything has
    /// published yet. A "not yet expired" result triggers a fresh scheduled publish
    /// (RescheduleCount + 1) rather than a simple ack/nack.
    /// </summary>
    private void SubscribeGameExpiryMessages(
        ICollection<IModel> channels,
        int consumerConcurrency,
        CancellationToken stoppingToken
    )
    {
        string queueName = QueueNames.ForType<GameTimeExpiredMessage>();
        string exchangeName = $"{queueName}.delayed";
        string retryExchangeName = $"{queueName}.retry";
        string deadLetterQueueName = $"{queueName}.dlq";

        for (int i = 0; i < consumerConcurrency; i++)
        {
            IModel channel = connection.CreateModel();
            channels.Add(channel);

            channel.ExchangeDeclare(
                exchangeName,
                type: "x-delayed-message",
                durable: true,
                arguments: new Dictionary<string, object> { ["x-delayed-type"] = "direct" }
            );
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(queueName, exchangeName, routingKey: queueName);
            DeclareRetryInfrastructure(channel, queueName, retryExchangeName, deadLetterQueueName);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            AsyncEventingBasicConsumer consumer = new(channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    string body = Encoding.UTF8.GetString(ea.Body.Span);
                    GameTimeExpiredMessage? message = JsonSerializer.Deserialize<GameTimeExpiredMessage>(body);

                    if (message is not null)
                    {
                        LogGameExpiryReceived(message.GameId, message.RescheduleCount);
                        await ProcessGameExpiryAsync(message, stoppingToken);
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

    private async Task ProcessGameExpiryAsync(GameTimeExpiredMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CompleteExpiredGameCommand(message.GameId, message.ExpectedStartedAt, message.RescheduleCount),
            cancellationToken
        );

        if (
            result.IsSuccess
            && result.Value.Outcome == CompleteExpiredGameOutcome.NotYetExpired_Rescheduled
            && result.Value.RescheduleForUtc is { } rescheduleFor
        )
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishScheduledAsync(
                message with
                {
                    RescheduleCount = message.RescheduleCount + 1,
                },
                rescheduleFor,
                cancellationToken
            );
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Received {MessageType}: {Message}")]
    private partial void LogMessageReceived(string messageType, string message);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Received GameTimeExpiredMessage for game {GameId} (reschedule count {RescheduleCount})"
    )]
    private partial void LogGameExpiryReceived(Guid gameId, int rescheduleCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message from RabbitMQ")]
    private partial void LogProcessingError(Exception ex);

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