using Ardalis.Result;
using FluentValidation;
using MediatR;
using StackDuel.Application.Events;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Factories;
using StackDuel.Domain.Submissions.ValueObjects;
using StackDuel.Domain.TestSuites;
using System.Linq;

namespace StackDuel.Application.Commands.Submissions.CreateSubmission;

internal sealed partial class CreateSubmissionHandler(
    IValidator<CreateSubmissionCommand> validator,
    IAggregateFactory<Submission, CreateSubmissionParams> submissionFactory,
    ISubmissionWriteRepository submissionRepository,
    ISubmissionJobRepository submissionJobRepository,
    IExecutionPipelineRepository pipelineRepository,
    ITestSuiteWriteRepository testSuiteRepository,
    IDomainEventDispatcher domainEventDispatcher
) : AbstractCommandHandler<CreateSubmissionCommand, Guid>(validator)
{
    /// <summary>
    /// Target total test case count for grading — not purely "random cases": public and
    /// hand-authored hidden cases always run, this only bounds how many random generated
    /// cases get added on top to reach it (see FindGradingTestCaseIdsByProblemSetupIdAsync).
    /// </summary>
    private const int OfficialGradingCaseCount = 20;

    protected override async Task<Result<Guid>> HandleValidated(
        CreateSubmissionCommand request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Guid> testCaseIds;
        if (request.Type == SubmissionType.Run)
        {
            var publicCaseIds = await testSuiteRepository.FindPublicTestCaseIdsByProblemSetupIdAsync(
                request.ProblemSetupId,
                cancellationToken
            );

            var customCaseIds = await testSuiteRepository.CreateAdHocTestCasesAsync(
                request.ProblemSetupId,
                request.CustomTestCases?.Select(tc => (IReadOnlyCollection<string>)tc.Inputs).ToArray() ?? [],
                cancellationToken
            );

            testCaseIds = [.. publicCaseIds, .. customCaseIds];
        }
        else
        {
            testCaseIds = await testSuiteRepository.FindGradingTestCaseIdsByProblemSetupIdAsync(
                request.ProblemSetupId,
                OfficialGradingCaseCount,
                cancellationToken
            );
        }

        if (testCaseIds.Count == 0)
            return Result<Guid>.Error("No test cases are available for this submission mode.");

        var pipelineId = await testSuiteRepository.FindPipelineIdByProblemSetupIdAsync(
            request.ProblemSetupId,
            cancellationToken
        );

        if (pipelineId is null)
            return Result<Guid>.NotFound("Submission pipeline is not available.");

        var pipeline = await pipelineRepository.FindByIdWithStepsAsync(pipelineId.Value, cancellationToken);
        if (pipeline is null)
            return Result<Guid>.NotFound("Submission pipeline is not available.");

        var firstStep = pipeline.FirstStep();
        if (firstStep is null)
            return Result<Guid>.Error("Submission pipeline is temporarily unavailable. Please try again later.");

        var submission = submissionFactory.Create(
            new CreateSubmissionParams(
                request.CreatedById,
                request.ProblemSetupId,
                request.Type,
                new SourceCode(request.Code),
                testCaseIds,
                request.AdditionalFiles?.Select(f => new SubmissionSourceFile(f.Path, f.Content)),
                request.GameId
            )
        );

        await submissionRepository.AddAsync(submission, cancellationToken);

        var job = new SubmissionJob(submission.Id, pipeline.Id, firstStep.Id);
        await submissionJobRepository.AddAsync(job, cancellationToken);

        await domainEventDispatcher.DispatchAsync(submission.PopDomainEvents(), cancellationToken);

        return Result<Guid>.Success(submission.Id);
    }
}