using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Api.Requests.Problem;

public sealed record GetMyProblemsRequest(
    int Page,
    int Size,
    DateTime Timestamp,
    ProblemStatus[]? Statuses,
    string? Search
);