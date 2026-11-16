using Microsoft.EntityFrameworkCore;
using StackDuel.Application.ProblemValidation;
using StackDuel.Application.ProblemValidation.Dtos;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.ProblemValidation;

internal sealed class ProblemValidationJobReadRepository(StackDuelDbContext context)
    : IProblemValidationJobReadRepository
{
    public async Task<ProblemValidationAttemptSummaryDto> GetAttemptSummaryAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    )
    {
        var attempts = await context
            .ProblemValidationJobs.AsNoTracking()
            .Where(job => job.ProblemId == problemId)
            .OrderByDescending(job => job.CreatedAt)
            .Select(job => new
            {
                job.CreatedAt,
                job.CompletedAt,
                job.Status,
                job.FailureReason,
            })
            .ToListAsync(cancellationToken);

        if (attempts.Count == 0)
            return ProblemValidationAttemptSummaryDto.NeverSubmitted;

        var latest = attempts[0];

        return new ProblemValidationAttemptSummaryDto(
            attempts.Count,
            latest.CreatedAt,
            latest.CompletedAt,
            latest.Status,
            latest.FailureReason
        );
    }
}
