using StackDuel.Application.Messaging;

namespace StackDuel.Application.Messaging.Messages;

/// <summary>
/// Published after a submission job's step completes (advances or needs a retry) so the
/// next step can be picked up immediately instead of waiting for the fallback cron sweep.
/// </summary>
public sealed record SubmissionJobContinuationMessage(Guid SubmissionId) : IMessage
{
    public static string QueueName => "submission-job-continuation";
}