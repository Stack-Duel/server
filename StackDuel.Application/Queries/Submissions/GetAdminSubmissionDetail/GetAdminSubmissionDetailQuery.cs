using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.Submissions.GetAdminSubmissionDetail;

public sealed record GetAdminSubmissionDetailQuery(Guid SubmissionId) : IQuery<AdminSubmissionDetailDto>;