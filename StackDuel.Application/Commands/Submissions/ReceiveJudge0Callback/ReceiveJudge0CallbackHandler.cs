using Ardalis.Result;
using StackDuel.Application.Commands;
using StackDuel.Application.Configuration;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;
using System.Security.Cryptography;
using System.Text;

namespace StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback;

internal sealed class ReceiveJudge0CallbackHandler(
    IMessagePublisher messagePublisher,
    ISubmissionReadRepository submissionReadRepository,
    Judge0Options judge0Options
) : ICommandHandler<ReceiveJudge0CallbackCommand>
{
    public async Task<Result> Handle(ReceiveJudge0CallbackCommand request, CancellationToken cancellationToken)
    {
        if (!judge0Options.UseCallback || string.IsNullOrEmpty(judge0Options.CallbackSecret))
            return Result.Unauthorized();

        if (!IsValidKey(request.Key, judge0Options.CallbackSecret))
            return Result.Unauthorized();

        if (request.SubmissionId == Guid.Empty)
            return Result.NotFound();

        SubmissionSummaryDto? summary = await submissionReadRepository.FindSummaryAsync(
            request.SubmissionId,
            cancellationToken
        );

        if (summary is null)
            return Result.NotFound();

        if (summary.Status is SubmissionStatus.Accepted or SubmissionStatus.WrongAnswer)
            return Result.Success();

        await messagePublisher.PublishAsync(
            new SubmissionJobContinuationMessage(request.SubmissionId),
            cancellationToken
        );

        return Result.Success();
    }

    private static bool IsValidKey(string? providedKey, string expectedKey)
    {
        if (providedKey is null)
            return false;

        byte[] provided = Encoding.UTF8.GetBytes(providedKey);
        byte[] expected = Encoding.UTF8.GetBytes(expectedKey);

        return provided.Length == expected.Length && CryptographicOperations.FixedTimeEquals(provided, expected);
    }
}