using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.Submissions.GetSubmissionProblemSnapshot;

public sealed record GetSubmissionProblemSnapshotQuery(Guid SubmissionId, Guid? UserId)
    : IQuery<SubmissionProblemSnapshotDto>;
