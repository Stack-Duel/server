using StackDuel.Application.Messaging;
using System.Text.Json;

namespace StackDuel.Infrastructure.Messaging;

/// <summary>
/// One queue a consumer listens on, paired with what to do with the messages that arrive.
/// Both transports iterate the same list so a new message type cannot be wired into one
/// consumer and forgotten in the other.
/// </summary>
internal sealed class MessageSubscription
{
    private readonly Func<ReadOnlyMemory<byte>, object?> _deserialize;
    private readonly Func<IServiceProvider, object, CancellationToken, Task> _handle;

    private MessageSubscription(
        Type messageType,
        string queueName,
        bool usesDelayedExchange,
        Func<ReadOnlyMemory<byte>, object?> deserialize,
        Func<IServiceProvider, object, CancellationToken, Task> handle
    )
    {
        MessageType = messageType;
        QueueName = queueName;
        UsesDelayedExchange = usesDelayedExchange;
        _deserialize = deserialize;
        _handle = handle;
    }

    public Type MessageType { get; }

    public string QueueName { get; }

    /// <summary>
    /// RabbitMQ only: the queue is also bound to an x-delayed-message exchange because this
    /// message is published with a scheduled delivery time. Azure Service Bus schedules
    /// natively and ignores this.
    /// </summary>
    public bool UsesDelayedExchange { get; }

    public object? Deserialize(ReadOnlyMemory<byte> body) => _deserialize(body);

    public Task HandleAsync(IServiceProvider serviceProvider, object message, CancellationToken cancellationToken) =>
        _handle(serviceProvider, message, cancellationToken);

    public static MessageSubscription For<TMessage>(
        Func<IServiceProvider, TMessage, CancellationToken, Task> handle,
        bool usesDelayedExchange = false
    )
        where TMessage : class, IMessage =>
        new(
            typeof(TMessage),
            TMessage.QueueName,
            usesDelayedExchange,
            body => JsonSerializer.Deserialize<TMessage>(body.Span),
            (serviceProvider, message, cancellationToken) =>
                handle(serviceProvider, (TMessage)message, cancellationToken)
        );
}