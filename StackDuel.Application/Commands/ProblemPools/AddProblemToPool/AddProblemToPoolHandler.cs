using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.AddProblemToPool;

internal sealed class AddProblemToPoolHandler(
    IProblemPoolRepository problemPoolRepository,
    IProblemReadRepository problemReadRepository,
    IValidator<AddProblemToPoolCommand> validator
) : AbstractCommandHandler<AddProblemToPoolCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        AddProblemToPoolCommand request,
        CancellationToken cancellationToken
    )
    {
        ProblemPool? pool = await problemPoolRepository.FindByKeyAsync(request.PoolKey, cancellationToken);
        if (pool is null)
            return Result.NotFound($"Pool '{request.PoolKey}' was not found.");

        bool problemExists = await problemReadRepository.ExistsForAdminAsync(request.ProblemId, cancellationToken);
        if (!problemExists)
            return Result.NotFound($"Problem '{request.ProblemId}' was not found.");

        pool.AddProblem(request.ProblemId);

        await problemPoolRepository.UpdateAsync(pool, cancellationToken);

        return Result.Success();
    }
}