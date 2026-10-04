namespace StackDuel.Application.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IMessage;

    /// <summary>
    /// Publishes a message for delayed delivery at (approximately) scheduledEnqueueTimeUtc.
    /// Returns a transport-specific handle that can be passed to CancelScheduledAsync to
    /// cancel delivery before it fires — on Azure Service Bus this is the message's sequence
    /// number. Not every transport can honor cancellation; consumers of scheduled messages
    /// should be idempotent and re-verify state rather than relying solely on cancellation.
    /// </summary>
    Task<long> PublishScheduledAsync<T>(
        T message,
        DateTimeOffset scheduledEnqueueTimeUtc,
        CancellationToken cancellationToken = default
    )
        where T : class, IMessage;

    /// <summary>
    /// Attempts to cancel a previously scheduled message using the handle returned from
    /// PublishScheduledAsync. Support varies by transport — RabbitMQ's delayed-message-exchange
    /// plugin has no cancel-by-handle API, so that implementation throws NotSupportedException.
    /// </summary>
    Task CancelScheduledAsync<T>(long sequenceNumber, CancellationToken cancellationToken = default)
        where T : class, IMessage;
}