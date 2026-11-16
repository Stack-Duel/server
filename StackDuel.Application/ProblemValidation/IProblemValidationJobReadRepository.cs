using StackDuel.Application.ProblemValidation.Dtos;

namespace StackDuel.Application.ProblemValidation;

public interface IProblemValidationJobReadRepository
{
    Task<ProblemValidationAttemptSummaryDto> GetAttemptSummaryAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    );
}
