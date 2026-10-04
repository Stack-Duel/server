using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.Submissions.GetSubmissionStatus;

public sealed record GetSubmissionStatusQuery(Guid SubmissionId, Guid UserId) : IQuery<SubmissionStatusDto>;