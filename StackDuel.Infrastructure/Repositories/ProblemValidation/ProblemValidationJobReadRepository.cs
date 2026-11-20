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
        int attemptCount = await context
            .ProblemValidationJobs.AsNoTracking()
            .CountAsync(job => job.ProblemId == problemId, cancellationToken);

        if (attemptCount == 0)
            return ProblemValidationAttemptSummaryDto.NeverSubmitted;

        var latest = await context
            .ProblemValidationJobs.AsNoTracking()
            .Where(job => job.ProblemId == problemId)
            .OrderByDescending(job => job.CreatedAt)
            .Select(job => new
            {
                job.CreatedAt,
                job.CompletedAt,
                job.Status,
                job.FailureReason,
                job.ProcessingAttempts,
                job.LastError,
                Steps = job
                    .Steps.OrderBy(step => step.Kind)
                    .Select(step => new ProblemValidationStepDto(
                        step.Kind,
                        step.Status,
                        step.StartedAt,
                        step.CompletedAt,
                        step.Detail,
                        step.Items.Select(item => new ProblemValidationStepItemDto(
                                item.Label,
                                item.Status,
                                item.Detail
                            ))
                            .ToList()
                    ))
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return ProblemValidationAttemptSummaryDto.NeverSubmitted;

        return new ProblemValidationAttemptSummaryDto(
            attemptCount,
            latest.CreatedAt,
            latest.CompletedAt,
            latest.Status,
            latest.FailureReason,
            latest.Steps,
            latest.ProcessingAttempts,
            latest.LastError
        );
    }
}
