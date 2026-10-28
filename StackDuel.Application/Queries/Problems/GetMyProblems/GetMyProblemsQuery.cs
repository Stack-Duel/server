using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetMyProblems;

public sealed record GetMyProblemsQuery(GetMyProblemsInput Input) : IQuery<PageResult<MyProblemListItemDto>>;