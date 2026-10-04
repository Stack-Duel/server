using Ardalis.Result;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.TestSuites;

namespace StackDuel.Application.Queries.Submissions.GetSubmissionStatus;

internal sealed class GetSubmissionStatusHandler(
    ISubmissionWriteRepository submissionRepository,
    ITestSuiteWriteRepository testSuiteRepository,
    ISubmissionJobRepository submissionJobRepository,
    IExecutionPipelineRepository pipelineRepository
) : IQueryHandler<GetSubmissionStatusQuery, SubmissionStatusDto>
{
    public async Task<Result<SubmissionStatusDto>> Handle(
        GetSubmissionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var submission = await submissionRepository.FindByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null || submission.UserId != request.UserId)
            return Result<SubmissionStatusDto>.NotFound();

        var testCaseIds = submission.Results.Select(result => result.TestCaseId).ToArray();

        var expectedOutputMap = await testSuiteRepository.FindExpectedOutputsByTestCaseIdsAsync(
            testCaseIds,
            cancellationToken
        );

        var inputMap = await testSuiteRepository.FindInputsByTestCaseIdsAsync(testCaseIds, cancellationToken);

        var results = submission
            .Results.Select(result => new SubmissionResultStatusDto(
                result.Status,
                result.Runtime,
                result.MemoryUsed,
                inputMap.GetValueOrDefault(result.TestCaseId),
                result.ActualOutput,
                expectedOutputMap.GetValueOrDefault(result.TestCaseId),
                result.StandardOutput,
                result.StandardError,
                result.CompileOutput
            ))
            .ToArray();

        string? currentStepName = null;
        var job = await submissionJobRepository.FindBySubmissionIdAsync(request.SubmissionId, cancellationToken);
        if (job?.CurrentStepId is not null)
        {
            var pipeline = await pipelineRepository.FindByIdWithStepsAsync(job.PipelineId, cancellationToken);
            currentStepName = pipeline?.Steps.FirstOrDefault(s => s.Id == job.CurrentStepId)?.Name;
        }

        return Result<SubmissionStatusDto>.Success(
            new SubmissionStatusDto(
                submission.Id,
                submission.ProblemSetupId,
                submission.Status,
                currentStepName,
                submission.SourceCode.Value,
                results
            )
        );
    }
}