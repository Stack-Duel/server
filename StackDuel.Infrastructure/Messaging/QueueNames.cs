using StackDuel.Application.Messaging;

namespace StackDuel.Infrastructure.Messaging;

internal static class QueueNames
{
    public static string ForType<T>()
        where T : IMessage => T.QueueName;
}