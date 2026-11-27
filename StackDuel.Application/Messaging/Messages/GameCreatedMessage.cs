using StackDuel.Application.Messaging;

namespace StackDuel.Application.Messaging.Messages;

public sealed record GameCreatedMessage(Guid GameId) : IMessage
{
    public static string QueueName => "game-created";
}