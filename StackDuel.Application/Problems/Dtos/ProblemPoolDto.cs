namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemPoolDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    int ProblemCount,
    DateTime CreatedAt
);