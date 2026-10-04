using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record CreateSubmissionCustomTestCaseDto(IReadOnlyCollection<string> Inputs);

public sealed record CreateSubmissionFileDto(string Path, string Content);

public sealed record CreateSubmissionDto(
    Guid ProblemSetupId,
    SubmissionType Type,
    string Code,
    Guid CreatedById,
    IReadOnlyCollection<CreateSubmissionCustomTestCaseDto>? CustomTestCases,
    IReadOnlyCollection<CreateSubmissionFileDto>? AdditionalFiles = null
);