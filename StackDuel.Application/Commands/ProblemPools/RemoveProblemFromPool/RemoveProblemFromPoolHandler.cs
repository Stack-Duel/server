using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.ProblemPools.RemoveProblemFromPool;

internal sealed class RemoveProblemFromPoolHandler(
    IProblemPoolRepository problemPoolRepository,
    IValidator<RemoveProblemFromPoolCommand> validator
) : AbstractCommandHandler<RemoveProblemFromPoolCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        RemoveProblemFromPoolCommand request,
        CancellationToken cancellationToken
    )
    {
        ProblemPool? pool = await problemPoolRepository.FindByKeyAsync(request.PoolKey, cancellationToken);
        if (pool is null)
            return Result.NotFound($"Pool '{request.PoolKey}' was not found.");

        pool.RemoveProblem(request.ProblemId);

        await problemPoolRepository.UpdateAsync(pool, cancellationToken);

        return Result.Success();
    }
}