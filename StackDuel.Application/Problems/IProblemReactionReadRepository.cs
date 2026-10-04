using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Problems;

public interface IProblemReactionReadRepository
{
    Task<ProblemReactionType?> FindReactionTypeByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<ProblemReactionSummaryDto> GetSummaryAsync(
        Guid problemId,
        Guid? userId,
        CancellationToken cancellationToken = default
    );
}