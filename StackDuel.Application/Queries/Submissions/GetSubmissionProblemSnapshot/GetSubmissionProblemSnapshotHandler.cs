using Ardalis.Result;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.Submissions.GetSubmissionProblemSnapshot;

internal sealed class GetSubmissionProblemSnapshotHandler(ISubmissionReadRepository submissionReadRepository)
    : IQueryHandler<GetSubmissionProblemSnapshotQuery, SubmissionProblemSnapshotDto>
{
    public async Task<Result<SubmissionProblemSnapshotDto>> Handle(
        GetSubmissionProblemSnapshotQuery request,
        CancellationToken cancellationToken
    )
    {
        SubmissionProblemSnapshotDto? snapshot = await submissionReadRepository.FindProblemSnapshotForSubmissionAsync(
            request.SubmissionId,
            request.UserId,
            cancellationToken
        );

        return snapshot is null ? Result<SubmissionProblemSnapshotDto>.NotFound() : Result.Success(snapshot);
    }
}