using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.Problems.SetProblemReaction;

internal sealed class SetProblemReactionHandler(
    IValidator<SetProblemReactionCommand> validator,
    IProblemReadRepository problemReadRepository,
    IProblemReactionReadRepository problemReactionReadRepository,
    IProblemReactionWriteRepository problemReactionWriteRepository
) : AbstractCommandHandler<SetProblemReactionCommand, ProblemReactionSummaryDto>(validator)
{
    protected override async Task<Result<ProblemReactionSummaryDto>> HandleValidated(
        SetProblemReactionCommand request,
        CancellationToken cancellationToken
    )
    {
        bool problemExists = await problemReadRepository.ExistsAsync(request.ProblemId, cancellationToken);
        if (!problemExists)
            return Result<ProblemReactionSummaryDto>.NotFound($"Problem '{request.ProblemId}' was not found.");

        ProblemReactionType? reactionType = await problemReactionReadRepository.FindReactionTypeByKeyAsync(
            request.ReactionTypeKey,
            cancellationToken
        );

        if (reactionType is null || !reactionType.IsEnabled)
            return Result<ProblemReactionSummaryDto>.NotFound(
                $"Reaction type '{request.ReactionTypeKey}' was not found."
            );

        ProblemReaction? targetReaction = await problemReactionWriteRepository.FindByProblemUserAndTypeAsync(
            request.ProblemId,
            request.UserId,
            reactionType.Id,
            cancellationToken
        );

        if (targetReaction is not null && targetReaction.IsActive)
        {
            // Same reaction clicked again — unlike/undislike. Only one reaction was ever active,
            // so nothing else needs to be touched.
            targetReaction.Deactivate();
            await problemReactionWriteRepository.UpdateAsync(targetReaction, cancellationToken);
        }
        else
        {
            // Activating a new or previously-unreacted kind. Clear whatever else is currently
            // active first so the DB's "one active reaction per user per problem" constraint is
            // never violated at any point either write commits.
            ProblemReaction? currentlyActive = await problemReactionWriteRepository.FindActiveByProblemAndUserAsync(
                request.ProblemId,
                request.UserId,
                cancellationToken
            );

            if (currentlyActive is not null && currentlyActive.Id != targetReaction?.Id)
            {
                currentlyActive.Deactivate();
                await problemReactionWriteRepository.UpdateAsync(currentlyActive, cancellationToken);
            }

            if (targetReaction is null)
            {
                targetReaction = new ProblemReaction(request.ProblemId, request.UserId, reactionType.Id);
                await problemReactionWriteRepository.AddAsync(targetReaction, cancellationToken);
            }
            else
            {
                targetReaction.Activate();
                await problemReactionWriteRepository.UpdateAsync(targetReaction, cancellationToken);
            }
        }

        var summary = await problemReactionReadRepository.GetSummaryAsync(
            request.ProblemId,
            request.UserId,
            cancellationToken
        );

        return Result<ProblemReactionSummaryDto>.Success(summary);
    }
}