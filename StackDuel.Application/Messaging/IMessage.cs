namespace StackDuel.Application.Messaging;

public interface IMessage
{
    static abstract string QueueName { get; }
}