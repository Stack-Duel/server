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

    private void Subscribe<TMessage>(
        ICollection<IModel> channels,
        Func<IServiceProvider, TMessage, CancellationToken, Task> handle,
        int consumerConcurrency,
        CancellationToken stoppingToken
    )
        where TMessage : IMessage
    {
        string queueName = QueueNames.ForType<TMessage>();

        for (int i = 0; i < consumerConcurrency; i++)
        {
            IModel channel = connection.CreateModel();
            channels.Add(channel);

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            AsyncEventingBasicConsumer consumer = new(channel);
            consumer.Received += async (_, ea) =>
            {
                string messageType = typeof(TMessage).Name;
                TMessage? message;

                try
                {
                    string body = Encoding.UTF8.GetString(ea.Body.Span);
                    message = JsonSerializer.Deserialize<TMessage>(body);
                }
                catch (JsonException ex)
                {
                    LogDeserializationFailed(ex, messageType);
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                if (message is null)
                {
                    LogNullMessage(messageType);
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                LogMessageReceived(messageType, message.ToString() ?? string.Empty);

                bool handled = await TryHandleWithBackoffAsync(
                    async () =>
                    {
                        using var scope = scopeFactory.CreateScope();
                        await handle(scope.ServiceProvider, message, stoppingToken);
                    },
                    messageType,
                    stoppingToken
                );

                if (handled)
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                else
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            };

            channel.BasicConsume(queueName, autoAck: false, consumer);
        }
    }

    private void SubscribeGameExpiryMessages(
        ICollection<IModel> channels,
        int consumerConcurrency,
        CancellationToken stoppingToken
    )
    {
        string queueName = QueueNames.ForType<GameTimeExpiredMessage>();
        string exchangeName = $"{queueName}.delayed";

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
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
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

    private async Task<bool> TryHandleWithBackoffAsync(
        Func<Task> handle,
        string messageType,
        CancellationToken cancellationToken
    )
    {
        for (int attempt = 1; attempt <= MaxDeliveryAttempts; attempt++)
        {
            try
            {
                await handle();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt == MaxDeliveryAttempts)
                {
                    LogProcessingAttemptsExhausted(ex, messageType, attempt);
                    return false;
                }

                TimeSpan delay = BackoffFor(attempt);
                LogProcessingRetry(ex, messageType, attempt, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        return false;
    }

    private static TimeSpan BackoffFor(int attempt)
    {
        double seconds = Math.Min(
            BaseRetryDelay.TotalSeconds * Math.Pow(2, attempt - 1),
            MaxRetryDelay.TotalSeconds
        );

        return TimeSpan.FromSeconds(seconds);
    }

    private const int MaxDeliveryAttempts = 4;
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Attempt {Attempt} to process {MessageType} failed, retrying in {DelaySeconds}s"
    )]
    private partial void LogProcessingRetry(
        Exception ex,
        string messageType,
        int attempt,
        double delaySeconds
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Giving up on {MessageType} after {Attempt} attempts; it will not be requeued"
    )]
    private partial void LogProcessingAttemptsExhausted(Exception ex, string messageType, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not deserialize {MessageType}; dropping it")]
    private partial void LogDeserializationFailed(Exception ex, string messageType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Received an empty {MessageType}; dropping it")]
    private partial void LogNullMessage(string messageType);
}