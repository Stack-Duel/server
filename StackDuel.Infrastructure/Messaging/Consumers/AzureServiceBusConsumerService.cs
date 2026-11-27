using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Configuration;
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

        var createdProcessors = new List<ServiceBusProcessor>();

        try
        {
            foreach (MessageSubscription subscription in MessageSubscriptions.All)
            {
                ServiceBusProcessor processor = client.CreateProcessor(subscription.QueueName, processorOptions);
                createdProcessors.Add(processor);
                WireUp(subscription, processor);
            }

            await Task.WhenAll(createdProcessors.Select(processor => processor.StartProcessingAsync(stoppingToken)));
        }
        catch (Exception ex)
        {
            LogStartupFailed(ex);
            foreach (var processor in createdProcessors)
            {
                await processor.DisposeAsync();
            }
            return;
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }

        foreach (var processor in createdProcessors)
        {
            await processor.StopProcessingAsync(CancellationToken.None);
        }

        foreach (var processor in createdProcessors)
        {
            await processor.DisposeAsync();
        }
    }

    private void WireUp(MessageSubscription subscription, ServiceBusProcessor processor)
    {
        processor.ProcessMessageAsync += async args =>
        {
            object? message;
            try
            {
                message = subscription.Deserialize(args.Message.Body.ToMemory());
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

            LogMessageReceived(subscription.MessageType.Name, message.ToString() ?? string.Empty);

            try
            {
                using var scope = scopeFactory.CreateScope();
                await subscription.HandleAsync(scope.ServiceProvider, message, args.CancellationToken);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message from Azure Service Bus")]
    private partial void LogProcessingError(Exception ex);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Failed to start Azure Service Bus processors")]
    private partial void LogStartupFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Azure Service Bus queue {EntityPath} does not exist; its processor has been stopped"
    )]
    private partial void LogQueueNotFound(string entityPath);
}