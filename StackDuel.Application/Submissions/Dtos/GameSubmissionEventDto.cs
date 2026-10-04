using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record GameSubmissionEventDto(
    Guid Id,
    Guid UserId,
    Guid ProblemId,
    string ProblemTitle,
    string ProblemSlug,
    SubmissionStatus Status,
    DateTime CreatedAt,
    string LanguageName
);