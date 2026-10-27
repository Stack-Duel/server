using StackDuel.Application.Pagination;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Problems.Dtos;

public sealed record GetMyProblemsInput(
    PaginationRequest PaginationRequest,
    IReadOnlyCollection<ProblemStatus> Statuses,
    string? Search
);