using Ardalis.Result;
using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Queries.ProblemPools.GetProblemPoolMemberIds;

internal sealed class GetProblemPoolMemberIdsHandler(IProblemPoolRepository problemPoolRepository)
    : IQueryHandler<GetProblemPoolMemberIdsQuery, IReadOnlyList<Guid>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(
        GetProblemPoolMemberIdsQuery request,
        CancellationToken cancellationToken
    )
    {
        ProblemPool? pool = await problemPoolRepository.FindByKeyAsync(request.PoolKey, cancellationToken);
        if (pool is null)
            return Result.NotFound($"Pool '{request.PoolKey}' was not found.");

        return Result.Success<IReadOnlyList<Guid>>([.. pool.ProblemIds]);
    }
}