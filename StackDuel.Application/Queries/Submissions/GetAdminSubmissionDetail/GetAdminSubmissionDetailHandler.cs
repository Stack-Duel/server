using Ardalis.Result;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.SubmissionJobs.Entities;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Domain.Submissions;

namespace StackDuel.Application.Queries.Submissions.GetAdminSubmissionDetail;

internal sealed class GetAdminSubmissionDetailHandler(
    ISubmissionWriteRepository submissionRepository,
    ISubmissionReadRepository submissionReadRepository,
    ISubmissionJobRepository submissionJobRepository,
    IExecutionPipelineRepository pipelineRepository
) : IQueryHandler<GetAdminSubmissionDetailQuery, AdminSubmissionDetailDto>
{
    public async Task<Result<AdminSubmissionDetailDto>> Handle(
        GetAdminSubmissionDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var submission = await submissionRepository.FindByIdAsync(request.SubmissionId, cancellationToken);
        if (submission is null)
            return Result<AdminSubmissionDetailDto>.NotFound();

        var context = await submissionReadRepository.FindAdminSubmissionByIdAsync(
            request.SubmissionId,
            cancellationToken
        );
        if (context is null)
            return Result<AdminSubmissionDetailDto>.NotFound();

        var results = submission
            .Results.Select(r => new AdminSubmissionResultDto(
                r.TestCaseId,
                r.Status,
                r.Runtime,
                r.MemoryUsed,
                r.ActualOutput,
                r.StandardOutput,
                r.StandardError,
                r.CompileOutput
            ))
            .ToArray();

        AdminSubmissionJobDto? jobDto = null;
        var job = await submissionJobRepository.FindBySubmissionIdAsync(request.SubmissionId, cancellationToken);
        if (job is not null)
        {
            var pipeline = await pipelineRepository.FindByIdWithStepsAsync(job.PipelineId, cancellationToken);
            var orderedSteps = pipeline?.Steps.OrderBy(s => s.StepOrder).ToArray() ?? [];

            var stepDtos = orderedSteps
                .Select(step =>
                {
                    var attempts = job
                        .Attempts.Where(a => a.PipelineStepId == step.Id)
                        .OrderBy(a => a.AttemptNumber)
                        .Select(a => new AdminSubmissionJobAttemptDto(
                            a.AttemptNumber,
                            a.Status,
                            a.RequestPayload,
                            a.ResponsePayload,
                            a.Error,
                            a.StartedAt,
                            a.CompletedAt,
                            a.DurationMs
                        ))
                        .ToArray();

                    bool isCurrent = job.CurrentStepId == step.Id;
                    var status = DeriveStepStatus(job, attempts, isCurrent);
                    int? totalDuration = attempts.Length > 0 ? attempts.Sum(a => a.DurationMs ?? 0) : null;

                    return new AdminSubmissionJobStepDto(
                        step.Id,
                        step.Name,
                        step.StepType,
                        step.StepOrder,
                        step.MaxAttempts,
                        step.TimeoutSeconds,
                        step.IsPolling,
                        isCurrent,
                        status,
                        attempts.Length,
                        totalDuration,
                        attempts
                    );
                })
                .ToArray();

            jobDto = new AdminSubmissionJobDto(
                job.Id,
                job.Status,
                job.FailureReason,
                job.CreatedAt,
                job.CompletedAt,
                job.CurrentStepId,
                stepDtos
            );
        }

        return Result.Success(
            new AdminSubmissionDetailDto(
                submission.Id,
                submission.Type,
                submission.Status,
                submission.SourceCode.Value,
                submission.CreatedAt,
                context.ProblemSetupId,
                context.ProblemId,
                context.ProblemTitle,
                context.ProblemSlug,
                context.Language,
                context.User,
                submission.Results.Max(r => r.MemoryUsed),
                submission.Results.Max(r => r.Runtime),
                results,
                jobDto
            )
        );
    }

    // SubmissionJob.Fail() leaves CurrentStepId pointing at the step that failed;
    // Complete() clears CurrentStepId to null. So a non-current step here has
    // necessarily already been passed (succeeded) or the job failed on it earlier
    // and CurrentStepId still points at it (making it "current" from this method's
    // perspective, not a past step) — the branches below rely on both behaviors.
    private static AdminSubmissionJobStepStatus DeriveStepStatus(
        SubmissionJob job,
        IReadOnlyList<AdminSubmissionJobAttemptDto> attempts,
        bool isCurrent
    )
    {
        if (attempts.Count == 0)
            return isCurrent ? AdminSubmissionJobStepStatus.Running : AdminSubmissionJobStepStatus.Pending;

        if (isCurrent)
            return job.Status == SubmissionJobStatus.Failed
                ? AdminSubmissionJobStepStatus.Failed
                : AdminSubmissionJobStepStatus.Running;

        var lastAttempt = attempts[^1];
        return lastAttempt.Status == SubmissionJobAttemptStatus.Succeeded
            ? AdminSubmissionJobStepStatus.Succeeded
            : AdminSubmissionJobStepStatus.Failed;
    }
}