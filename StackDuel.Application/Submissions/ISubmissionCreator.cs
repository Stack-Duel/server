using Ardalis.Result;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions;

public sealed record SubmissionCreationRequest(
    Guid ProblemSetupId,
    SubmissionType Type,
    string Code,
    Guid CreatedById,
    IReadOnlyCollection<CreateSubmissionCustomTestCaseDto>? CustomTestCases = null,
    IReadOnlyCollection<CreateSubmissionFileDto>? AdditionalFiles = null,
    Guid? GameId = null
);

/// <summary>
/// Creates a submission together with the pipeline job that grades it: selects the test cases
/// the submission type calls for, persists the aggregate, queues the job and dispatches the
/// resulting domain events. Shared by the CreateSubmission command and by game submissions,
/// which need the same work done as part of their own transaction rather than by dispatching a
/// second command.
/// </summary>
public interface ISubmissionCreator
{
    Task<Result<Guid>> CreateAsync(SubmissionCreationRequest request, CancellationToken cancellationToken = default);
}