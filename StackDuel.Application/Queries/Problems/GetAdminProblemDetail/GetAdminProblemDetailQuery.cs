using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetAdminProblemDetail;

public sealed record GetAdminProblemDetailQuery(Guid ProblemId) : IQuery<AdminProblemDetailDto>;