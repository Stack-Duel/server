using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Services.Problems;

public interface IProblemLimitService
{
    Task<bool> HasReachedOpenProblemLimitAsync(Guid creatorId, CancellationToken cancellationToken = default);
}

internal sealed class ProblemLimitService(IProblemReadRepository problemReadRepository) : IProblemLimitService
{
    public async Task<bool> HasReachedOpenProblemLimitAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default
    )
    {
        int openCount = await problemReadRepository.CountUnresolvedProblemsByCreatorAsync(
            creatorId,
            cancellationToken
        );

        return openCount >= Problem.MaxOpenProblemsPerCreator;
    }
}