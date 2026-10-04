using StackDuel.Application.Commands;
using StackDuel.Application.Configuration;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using Ardalis.Result;
using System.Security.Cryptography;
using System.Text;

namespace StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback;

internal sealed class ReceiveJudge0CallbackHandler(IMessagePublisher messagePublisher, Judge0Options judge0Options)
    : ICommandHandler<ReceiveJudge0CallbackCommand>
{
    public async Task<Result> Handle(ReceiveJudge0CallbackCommand request, CancellationToken cancellationToken)
    {
        if (!judge0Options.UseCallback || string.IsNullOrEmpty(judge0Options.CallbackSecret))
            return Result.Unauthorized();

        if (!IsValidKey(request.Key, judge0Options.CallbackSecret))
            return Result.Unauthorized();

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