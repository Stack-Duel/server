using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Problems.Dtos;

public sealed record MyProblemListItemDto
{
    public required Guid Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required int DifficultyValue { get; init; }
    public required DifficultyTier DifficultyTier { get; init; }
    public required ProblemStatus Status { get; init; }
    public required int TimeLimitMs { get; init; }
    public required int MemoryLimitMb { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required IReadOnlyList<string> Languages { get; init; }
    public required int SetupCount { get; init; }
    public required DateTime CreatedAt { get; init; }
}