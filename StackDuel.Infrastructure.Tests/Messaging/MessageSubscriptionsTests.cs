using StackDuel.Application.Messaging;
using StackDuel.Infrastructure.Messaging;

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
}