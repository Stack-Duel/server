using StackDuel.Application.Messaging;

namespace StackDuel.Application.Messaging.Messages;

public sealed record ProblemValidationJobContinuationMessage(Guid JobId) : IMessage
{
    public static string QueueName => "problem-validation-job-continuation";
}