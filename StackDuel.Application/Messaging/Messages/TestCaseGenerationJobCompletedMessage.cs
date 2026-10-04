using StackDuel.Application.Messaging;

namespace StackDuel.Application.Messaging.Messages;

public sealed record TestCaseGenerationJobCompletedMessage(Guid JobId) : IMessage
{
    public static string QueueName => "test-case-generation-job-completed";
}