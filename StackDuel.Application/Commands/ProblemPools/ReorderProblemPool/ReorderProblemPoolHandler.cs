using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.ProblemPools.ReorderProblemPool;

internal sealed class ReorderProblemPoolHandler(
    IProblemPoolRepository problemPoolRepository,
    IValidator<ReorderProblemPoolCommand> validator
) : AbstractCommandHandler<ReorderProblemPoolCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        ReorderProblemPoolCommand request,
        CancellationToken cancellationToken
    )
    {
        ProblemPool? pool = await problemPoolRepository.FindByKeyAsync(request.PoolKey, cancellationToken);
        if (pool is null)
            return Result.NotFound($"Pool '{request.PoolKey}' was not found.");

        HashSet<Guid> currentMemberIds = [.. pool.ProblemIds];
        HashSet<Guid> requestedIds = [.. request.ProblemIds];

        if (requestedIds.Count != request.ProblemIds.Count || !currentMemberIds.SetEquals(requestedIds))
        {
            return Result.Invalid(
                new ValidationError(
                    nameof(request.ProblemIds),
                    "The given order must include every current pool member exactly once."
                )
            );
        }

        pool.Reorder(request.ProblemIds);

        await problemPoolRepository.UpdateAsync(pool, cancellationToken);

        return Result.Success();
    }
}