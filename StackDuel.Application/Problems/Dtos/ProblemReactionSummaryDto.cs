namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemReactionCountDto(string Key, string Name, string? Emoji, int Count);

public sealed record ProblemReactionSummaryDto(
    IReadOnlyList<ProblemReactionCountDto> Counts,
    string? CurrentUserReactionKey
);