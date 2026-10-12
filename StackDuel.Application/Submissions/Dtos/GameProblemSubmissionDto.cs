using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record GameProblemSubmissionDto(
    Guid SubmissionId,
    Guid GameId,
    Guid UserId,
    Guid ProblemId,
    SubmissionStatus Status,
    DateTime CreatedAt
);
