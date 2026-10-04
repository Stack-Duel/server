using Ardalis.Result;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Queries.ProblemPools.GetOrderedProblemPoolMembers;

internal sealed class GetOrderedProblemPoolMembersHandler(
    IProblemPoolRepository problemPoolRepository,
    IProblemReadRepository problemReadRepository
) : IQueryHandler<GetOrderedProblemPoolMembersQuery, IReadOnlyList<AdminProblemListRowDto>>
{
    public async Task<Result<IReadOnlyList<AdminProblemListRowDto>>> Handle(
        GetOrderedProblemPoolMembersQuery request,
        CancellationToken cancellationToken
    )
    {
        ProblemPool? pool = await problemPoolRepository.FindByKeyAsync(request.PoolKey, cancellationToken);
        if (pool is null)
            return Result.NotFound($"Pool '{request.PoolKey}' was not found.");

        IReadOnlyCollection<Guid> orderedIds = pool.ProblemIds;
        var rows = await problemReadRepository.FindByIdsAsync(orderedIds, cancellationToken);
        Dictionary<Guid, AdminProblemListRowDto> rowsById = rows.ToDictionary(row => row.Id);

        return Result.Success<IReadOnlyList<AdminProblemListRowDto>>(
            [.. orderedIds.Where(rowsById.ContainsKey).Select(id => rowsById[id])]
        );
    }
}