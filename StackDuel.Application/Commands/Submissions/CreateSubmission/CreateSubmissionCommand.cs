using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Commands.Submissions.CreateSubmission;

using StackDuel.Application.Submissions.Dtos;

internal sealed record CreateSubmissionCommand(
    Guid ProblemSetupId,
    SubmissionType Type,
    string Code,
    Guid CreatedById,
    IReadOnlyCollection<CreateSubmissionCustomTestCaseDto>? CustomTestCases,
    IReadOnlyCollection<CreateSubmissionFileDto>? AdditionalFiles = null,
    Guid? GameId = null
) : ICommand<Guid>;