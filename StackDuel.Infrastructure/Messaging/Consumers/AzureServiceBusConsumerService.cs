using StackDuel.Application.Commands.Games.CompleteExpiredGame;
using StackDuel.Application.Configuration;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Infrastructure.Jobs.ProblemValidation;
using StackDuel.Infrastructure.Jobs.Submissions;
using StackDuel.Infrastructure.Jobs.TestCaseGeneration;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace StackDuel.Infrastructure.Messaging.Consumers;

internal sealed partial class AzureServiceBusConsumerService(
    ServiceBusClient client,
    MessageBusOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<AzureServiceBusConsumerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int maxConcurrentCalls = Math.Max(1, options.AzureServiceBus.MaxConcurrentCalls);
        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = maxConcurrentCalls,
            AutoCompleteMessages = false,
        };

        var createdProcessor = client.CreateProcessor(QueueNames.ForType<SubmissionCreatedMessage>(), processorOptions);
        var continuationProcessor = client.CreateProcessor(
            QueueNames.ForType<SubmissionJobContinuationMessage>(),
            processorOptions
        );
        var problemValidationProcessor = client.CreateProcessor(
            QueueNames.ForType<ProblemValidationJobContinuationMessage>(),
            processorOptions
        );
        var testCaseGenerationContinuationProcessor = client.CreateProcessor(
            QueueNames.ForType<TestCaseGenerationJobContinuationMessage>(),
            processorOptions
        );
        var testCaseGenerationCompletedProcessor = client.CreateProcessor(
            QueueNames.ForType<TestCaseGenerationJobCompletedMessage>(),
            processorOptions
        );
        var gameExpiryProcessor = client.CreateProcessor(
            QueueNames.ForType<GameTimeExpiredMessage>(),
            processorOptions
        );

        WireUp<SubmissionCreatedMessage>(
            createdProcessor,
            (sp, m, ct) =>
                sp.GetRequiredService<SubmissionJobProcessorService>().RunForSubmissionAsync(m.SubmissionId, ct)
        );

        WireUp<SubmissionJobContinuationMessage>(
            continuationProcessor,
            (sp, m, ct) =>
                sp.GetRequiredService<SubmissionJobProcessorService>().RunForSubmissionAsync(m.SubmissionId, ct)
        );

        WireUp<ProblemValidationJobContinuationMessage>(
            problemValidationProcessor,
            (sp, m, ct) =>
                sp.GetRequiredService<ProblemValidationJobProcessorService>().RunPendingForJobAsync(m.JobId, ct)
        );

        WireUp<TestCaseGenerationJobContinuationMessage>(
            testCaseGenerationContinuationProcessor,
            (sp, m, ct) => sp.GetRequiredService<TestCaseGenerationJobProcessorService>().RunForJobAsync(m.JobId, ct)
        );

        WireUp<TestCaseGenerationJobCompletedMessage>(
            testCaseGenerationCompletedProcessor,
            (sp, _, ct) =>
                sp.GetRequiredService<ProblemValidationJobProcessorService>().RunAwaitingGenerationPassAsync(ct)
        );

        WireUpGameExpiry(gameExpiryProcessor);

        await Task.WhenAll(
            createdProcessor.StartProcessingAsync(stoppingToken),
            continuationProcessor.StartProcessingAsync(stoppingToken),
            problemValidationProcessor.StartProcessingAsync(stoppingToken),
            testCaseGenerationContinuationProcessor.StartProcessingAsync(stoppingToken),
            testCaseGenerationCompletedProcessor.StartProcessingAsync(stoppingToken),
            gameExpiryProcessor.StartProcessingAsync(stoppingToken)
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }

        await createdProcessor.StopProcessingAsync(CancellationToken.None);
        await continuationProcessor.StopProcessingAsync(CancellationToken.None);
        await problemValidationProcessor.StopProcessingAsync(CancellationToken.None);
        await testCaseGenerationContinuationProcessor.StopProcessingAsync(CancellationToken.None);
        await testCaseGenerationCompletedProcessor.StopProcessingAsync(CancellationToken.None);
        await gameExpiryProcessor.StopProcessingAsync(CancellationToken.None);
        await createdProcessor.DisposeAsync();
        await continuationProcessor.DisposeAsync();
        await problemValidationProcessor.DisposeAsync();
        await testCaseGenerationContinuationProcessor.DisposeAsync();
        await testCaseGenerationCompletedProcessor.DisposeAsync();
        await gameExpiryProcessor.DisposeAsync();
    }

    private void WireUp<TMessage>(
        ServiceBusProcessor processor,
        Func<IServiceProvider, TMessage, CancellationToken, Task> handle
    )
        where TMessage : IMessage
    {
        processor.ProcessMessageAsync += async args =>
        {
            TMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<TMessage>(args.Message.Body);
            }
            catch (JsonException ex)
            {
                LogProcessingError(ex);
                await args.DeadLetterMessageAsync(args.Message, deadLetterReason: "DeserializationFailed");
                return;
            }

            if (message is null)
            {
                await args.DeadLetterMessageAsync(args.Message, deadLetterReason: "NullMessage");
                return;
            }

            LogMessageReceived(typeof(TMessage).Name, message.ToString() ?? string.Empty);

            try
            {
                using var scope = scopeFactory.CreateScope();
                await handle(scope.ServiceProvider, message, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                LogProcessingError(ex);
                await args.AbandonMessageAsync(args.Message);
            }
        };

        processor.ProcessErrorAsync += async args =>
        {
            if (args.Exception is ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityNotFound })
            {
                LogQueueNotFound(args.EntityPath);
                await processor.StopProcessingAsync();
                return;
            }

            LogProcessingError(args.Exception);
        };
    }

    /// <summary>
    /// Unlike WireUp&lt;T&gt;, this sends a mediator command whose result can indicate
    /// "arrived early" — in which case this re-publishes a scheduled message with
    /// RescheduleCount + 1 rather than treating it as a terminal outcome.
    /// </summary>
    private void WireUpGameExpiry(ServiceBusProcessor processor)
    {
        processor.ProcessMessageAsync += async args =>
        {
            GameTimeExpiredMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<GameTimeExpiredMessage>(args.Message.Body);
            }
            catch (JsonException ex)
            {
                LogProcessingError(ex);
                await args.DeadLetterMessageAsync(args.Message, deadLetterReason: "DeserializationFailed");
                return;
            }

            if (message is null)
            {
                await args.DeadLetterMessageAsync(args.Message, deadLetterReason: "NullMessage");
                return;
            }

            LogGameExpiryReceived(message.GameId, message.RescheduleCount);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new CompleteExpiredGameCommand(message.GameId, message.ExpectedStartedAt, message.RescheduleCount),
                    args.CancellationToken
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
                        args.CancellationToken
                    );
                }

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                LogProcessingError(ex);
                await args.AbandonMessageAsync(args.Message);
            }
        };

        processor.ProcessErrorAsync += async args =>
        {
            if (args.Exception is ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityNotFound })
            {
                LogQueueNotFound(args.EntityPath);
                await processor.StopProcessingAsync();
                return;
            }

            LogProcessingError(args.Exception);
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Received {MessageType}: {Message}")]
    private partial void LogMessageReceived(string messageType, string message);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Received GameTimeExpiredMessage for game {GameId} (reschedule count {RescheduleCount})"
    )]
    private partial void LogGameExpiryReceived(Guid gameId, int rescheduleCount);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Azure Service Bus queue '{QueueName}' not found — consumer stopped. Check queue name configuration."
    )]
    private partial void LogQueueNotFound(string QueueName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message from Azure Service Bus")]
    private partial void LogProcessingError(Exception ex);
}