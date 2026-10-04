using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.ProblemPools.AddProblemsToPool;

internal sealed class AddProblemsToPoolHandler(
    IProblemPoolRepository problemPoolRepository,
    IProblemReadRepository problemReadRepository,
    IValidator<AddProblemsToPoolCommand> validator
) : AbstractCommandHandler<AddProblemsToPoolCommand, int>(validator)
{
    protected override async Task<Result<int>> HandleValidated(
        AddProblemsToPoolCommand request,
        CancellationToken cancellationToken
    )
    {
        ProblemPool? pool = await problemPoolRepository.FindByKeyAsync(request.PoolKey, cancellationToken);
        if (pool is null)
            return Result<int>.NotFound($"Pool '{request.PoolKey}' was not found.");

        IReadOnlyList<Guid> idsToAdd;
        if (request.SelectAllMatching)
        {
            IReadOnlyList<Guid> matchingIds = await problemReadRepository.GetAdminProblemIdsMatchingAsync(
                request.Search,
                cancellationToken
            );
            HashSet<Guid> excluded = [.. request.ExcludedProblemIds];
            idsToAdd = [.. matchingIds.Where(id => !excluded.Contains(id))];
        }
        else
        {
            var existing = await problemReadRepository.FindByIdsAsync(request.ProblemIds, cancellationToken);
            idsToAdd = [.. existing.Select(p => p.Id)];
        }

        int before = pool.ProblemIds.Count;
        foreach (Guid problemId in idsToAdd)
            pool.AddProblem(problemId);

        await problemPoolRepository.UpdateAsync(pool, cancellationToken);

        return Result<int>.Success(pool.ProblemIds.Count - before);
    }
}