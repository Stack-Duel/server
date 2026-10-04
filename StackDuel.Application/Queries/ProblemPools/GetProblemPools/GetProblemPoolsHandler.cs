using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using Ardalis.Result;

namespace StackDuel.Application.Queries.ProblemPools.GetProblemPools;

internal sealed class GetProblemPoolsHandler(IProblemPoolRepository problemPoolRepository)
    : IQueryHandler<GetProblemPoolsQuery, IReadOnlyList<ProblemPoolDto>>
{
    public async Task<Result<IReadOnlyList<ProblemPoolDto>>> Handle(
        GetProblemPoolsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ProblemPoolDto> pools = await problemPoolRepository.GetAllAsync(cancellationToken);

        return Result<IReadOnlyList<ProblemPoolDto>>.Success(pools);
    }
}