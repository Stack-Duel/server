namespace StackDuel.Application.Problems.Dtos;

public sealed record MyProblemsMetadataDto
{
    public required int TotalSubmissions { get; init; }

    public required int TotalReactions { get; init; }

    public required IReadOnlyList<ProblemReactionCountDto> ReactionBreakdown { get; init; }

    public required IReadOnlyList<ProblemStatusCountDto> StatusBreakdown { get; init; }

    public required double AverageCompletionRate { get; init; }
}