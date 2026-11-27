using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Infrastructure.Messaging;
using System.Text.Json;

namespace StackDuel.Infrastructure.Tests.Messaging;

public class MessageSubscriptionsTests
{
    private static IReadOnlyList<Type> AllMessageTypes() =>
        [
            .. typeof(IMessage)
                .Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IMessage).IsAssignableFrom(type))
                .OrderBy(type => type.Name),
        ];

    [Fact]
    public void EveryMessageType_HasASubscription()
    {
        IReadOnlyList<string> unsubscribed =
        [
            .. AllMessageTypes()
                .Where(type => !MessageSubscriptions.All.Any(subscription => subscription.MessageType == type))
                .Select(type => type.Name),
        ];

        Assert.Empty(unsubscribed);
    }

    [Fact]
    public void NoMessageType_IsSubscribedTwice()
    {
        IReadOnlyList<string> duplicated =
        [
            .. MessageSubscriptions
                .All.GroupBy(subscription => subscription.MessageType)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.Name),
        ];

        Assert.Empty(duplicated);
    }

    [Fact]
    public void EverySubscription_HasADistinctQueueName()
    {
        IReadOnlyList<string> duplicated =
        [
            .. MessageSubscriptions
                .All.GroupBy(subscription => subscription.QueueName)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key),
        ];

        Assert.Empty(duplicated);
    }

    [Fact]
    public void EverySubscription_HasANonEmptyQueueName()
    {
        Assert.All(MessageSubscriptions.All, subscription => Assert.False(string.IsNullOrWhiteSpace(subscription.QueueName)));
    }

    [Fact]
    public void Deserialize_RoundTripsTheSerializedWireFormat()
    {
        MessageSubscription subscription = MessageSubscriptions.All.Single(s =>
            s.MessageType == typeof(GameCreatedMessage)
        );
        GameCreatedMessage original = new(Guid.NewGuid());

        object? deserialized = subscription.Deserialize(JsonSerializer.SerializeToUtf8Bytes(original));

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void OnlyTheScheduledMessage_UsesTheDelayedExchange()
    {
        IReadOnlyList<string> delayed =
        [
            .. MessageSubscriptions.All.Where(s => s.UsesDelayedExchange).Select(s => s.MessageType.Name),
        ];

        Assert.Equal([nameof(GameTimeExpiredMessage)], delayed);
    }
}