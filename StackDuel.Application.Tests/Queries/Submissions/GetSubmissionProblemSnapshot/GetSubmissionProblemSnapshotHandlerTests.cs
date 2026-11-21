using Ardalis.Result;
using Moq;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using GetSubmissionProblemSnapshotHandler = StackDuel.Application.Queries.Submissions.GetSubmissionProblemSnapshot.GetSubmissionProblemSnapshotHandler;
using GetSubmissionProblemSnapshotQuery = StackDuel.Application.Queries.Submissions.GetSubmissionProblemSnapshot.GetSubmissionProblemSnapshotQuery;

namespace StackDuel.Application.Tests.Queries.Submissions.GetSubmissionProblemSnapshot;

public class GetSubmissionProblemSnapshotHandlerTests
{
    private readonly Mock<ISubmissionReadRepository> _submissionReadRepository = new();
    private readonly GetSubmissionProblemSnapshotHandler _handler;

    public GetSubmissionProblemSnapshotHandlerTests()
    {
        _handler = new GetSubmissionProblemSnapshotHandler(_submissionReadRepository.Object);
    }

    private static SubmissionProblemSnapshotDto Snapshot(int? versionNumber) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "two-sum",
            versionNumber,
            "Two Sum",
            "Given an array...",
            500,
            1000,
            128,
            "def solve():",
            "solve"
        );

    private void SetUpSnapshot(SubmissionProblemSnapshotDto? snapshot) =>
        _submissionReadRepository
            .Setup(repository =>
                repository.FindProblemSnapshotForSubmissionAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(snapshot);

    [Fact]
    public async Task Handle_UnknownSubmission_ReturnsNotFound()
    {
        SetUpSnapshot(null);

        Result<SubmissionProblemSnapshotDto> result = await _handler.Handle(
            new GetSubmissionProblemSnapshotQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_PinnedSubmission_ReportsTheVersionItRanOn()
    {
        SetUpSnapshot(Snapshot(versionNumber: 3));

        Result<SubmissionProblemSnapshotDto> result = await _handler.Handle(
            new GetSubmissionProblemSnapshotQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.VersionNumber);
        Assert.True(result.Value.IsPinnedToAVersion);
    }

    [Fact]
    public async Task Handle_LegacySubmission_IsReportedAsUnpinned()
    {
        SetUpSnapshot(Snapshot(versionNumber: null));

        Result<SubmissionProblemSnapshotDto> result = await _handler.Handle(
            new GetSubmissionProblemSnapshotQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPinnedToAVersion);
    }

    [Fact]
    public async Task Handle_PassesTheRequestingUserThrough()
    {
        Guid submissionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        SetUpSnapshot(Snapshot(versionNumber: 1));

        await _handler.Handle(new GetSubmissionProblemSnapshotQuery(submissionId, userId), CancellationToken.None);

        _submissionReadRepository.Verify(
            repository =>
                repository.FindProblemSnapshotForSubmissionAsync(submissionId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}