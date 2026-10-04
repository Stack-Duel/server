namespace StackDuel.Application.Games.Dtos;

public enum AdminGameHistoryEventType
{
    Accepted,
    WrongAnswer,
    Skipped,
}

public sealed record AdminGameHistoryEventDto(
    AdminGameHistoryEventType Type,
    Guid ProblemId,
    string ProblemTitle,
    string ProblemSlug,
    DateTime? OccurredAt,
    Guid? SubmissionId,
    string? LanguageName
);

public sealed record AdminGamePlayerHistoryDto(Guid UserId, IReadOnlyList<AdminGameHistoryEventDto> Events);