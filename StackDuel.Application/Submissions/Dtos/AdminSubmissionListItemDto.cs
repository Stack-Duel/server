using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record AdminSubmissionListItemDto(
    Guid Id,
    SubmissionType Type,
    SubmissionStatus Status,
    Guid ProblemSetupId,
    Guid ProblemId,
    string ProblemTitle,
    string ProblemSlug,
    SubmissionLanguageDto Language,
    SubmissionUserDto User,
    DateTime CreatedAt,
    int? MemoryUsage,
    int? ExecutionTime
);