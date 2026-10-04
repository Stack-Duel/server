using Azure.Messaging.ServiceBus;
using StackDuel.Application.Messaging;
using System.Text.Json;

namespace StackDuel.Infrastructure.Messaging;

internal sealed class AzureServiceBusMessagePublisher(ServiceBusClient client) : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IMessage
    {
        await using var sender = client.CreateSender(QueueNames.ForType<T>());
        await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(message)), cancellationToken);
    }

    /// <summary>
    /// Schedules delayed delivery natively via Azure Service Bus. Returns the sequence number
    /// so the caller can cancel it later (e.g. if the game is forfeited before its time limit
    /// elapses) via CancelScheduledAsync.
    /// </summary>
    public async Task<long> PublishScheduledAsync<T>(
        T message,
        DateTimeOffset scheduledEnqueueTimeUtc,
        CancellationToken cancellationToken = default
    )
        where T : class, IMessage
    {
        await using var sender = client.CreateSender(QueueNames.ForType<T>());
        var sbMessage = new ServiceBusMessage(JsonSerializer.Serialize(message));
        return await sender.ScheduleMessageAsync(sbMessage, scheduledEnqueueTimeUtc, cancellationToken);
    }

    public async Task CancelScheduledAsync<T>(long sequenceNumber, CancellationToken cancellationToken = default)
        where T : class, IMessage
    {
        await using var sender = client.CreateSender(QueueNames.ForType<T>());

        try
        {
            await sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
        {
            // Already delivered/expired/cancelled — nothing to do. The consumer's own
            // idempotency check is the real safety net here, not this cancel call succeeding.
        }
    }
}