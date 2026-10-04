using StackDuel.Application.Messaging;

namespace StackDuel.Application.Messaging.Messages;

public sealed record SubmissionCreatedMessage(Guid SubmissionId) : IMessage
{
    public static string QueueName => "submission-created";
}