using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record SubmissionSummaryDto(
    Guid UserId,
    Guid ProblemSetupId,
    SubmissionType Type,
    SubmissionStatus Status
);