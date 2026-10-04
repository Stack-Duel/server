using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Problems;
using StackDuel.Domain.DailyChallenges.Entities;

namespace StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge;

internal sealed class UpdateDailyChallengeHandler(
    IDailyChallengeRepository dailyChallengeRepository,
    IProblemReadRepository problemReadRepository,
    IValidator<UpdateDailyChallengeCommand> validator
) : AbstractCommandHandler<UpdateDailyChallengeCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateDailyChallengeCommand request,
        CancellationToken cancellationToken
    )
    {
        DailyChallenge? challenge = await dailyChallengeRepository.FindByDateAsync(request.Date, cancellationToken);
        if (challenge is null)
            return Result.NotFound($"No daily challenge is scheduled for {request.Date}.");

        if (!await problemReadRepository.ExistsForAdminAsync(request.ProblemId, cancellationToken))
            return Result.Invalid(new ValidationError(nameof(request.ProblemId), "Problem was not found."));

        challenge.UpdateProblem(request.ProblemId);

        await dailyChallengeRepository.UpdateAsync(challenge, cancellationToken);

        return Result.Success();
    }
}