using StackDuel.Application.Messaging;

namespace StackDuel.Application.Messaging.Messages;

public sealed record TestCaseGenerationJobContinuationMessage(Guid JobId) : IMessage
{
    public static string QueueName => "test-case-generation-job-continuation";
}