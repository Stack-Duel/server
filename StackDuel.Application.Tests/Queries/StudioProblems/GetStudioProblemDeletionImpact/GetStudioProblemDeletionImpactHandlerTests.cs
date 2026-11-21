using Ardalis.Result;
using Moq;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using GetStudioProblemDeletionImpactHandler = StackDuel.Application.Queries.StudioProblems.GetStudioProblemDeletionImpact.GetStudioProblemDeletionImpactHandler;
using GetStudioProblemDeletionImpactQuery = StackDuel.Application.Queries.StudioProblems.GetStudioProblemDeletionImpact.GetStudioProblemDeletionImpactQuery;

namespace StackDuel.Application.Tests.Queries.StudioProblems.GetStudioProblemDeletionImpact;

public class GetStudioProblemDeletionImpactHandlerTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly GetStudioProblemDeletionImpactHandler _handler;

    public GetStudioProblemDeletionImpactHandlerTests()
    {
        _handler = new GetStudioProblemDeletionImpactHandler(_problemReadRepository.Object);
    }

    private void SetUpOwner(Guid problemId, Guid? ownerId) =>
        _problemReadRepository
            .Setup(repository => repository.GetCreatedByIdAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownerId);

    private void SetUpImpact(Guid problemId, ProblemDeletionImpactDto impact) =>
        _problemReadRepository
            .Setup(repository => repository.FindDeletionImpactAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(impact);

    [Fact]
    public async Task AnUnknownProblem_ReturnsNotFound()
    {
        Guid problemId = Guid.NewGuid();
        SetUpOwner(problemId, null);

        Result<ProblemDeletionImpactDto> result = await _handler.Handle(
            new GetStudioProblemDeletionImpactQuery(problemId, Guid.NewGuid()),
            default
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task SomeoneElsesProblem_ReturnsNotFoundRatherThanLeakingItExists()
    {
        Guid problemId = Guid.NewGuid();
        SetUpOwner(problemId, Guid.NewGuid());

        Result<ProblemDeletionImpactDto> result = await _handler.Handle(
            new GetStudioProblemDeletionImpactQuery(problemId, Guid.NewGuid()),
            default
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _problemReadRepository.Verify(
            repository => repository.FindDeletionImpactAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task TheOwner_GetsTheImpact()
    {
        Guid problemId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        SetUpOwner(problemId, ownerId);
        SetUpImpact(problemId, new ProblemDeletionImpactDto(7, 3, 2, 1, 0, 1));

        Result<ProblemDeletionImpactDto> result = await _handler.Handle(
            new GetStudioProblemDeletionImpactQuery(problemId, ownerId),
            default
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.SubmissionCount);
        Assert.Equal(2, result.Value.UnfinishedGameCount);
        Assert.True(result.Value.IsInPlay);
        Assert.True(result.Value.HasHistory);
    }

    [Fact]
    public async Task AnUntouchedProblem_ReportsNothingInPlay()
    {
        Guid problemId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        SetUpOwner(problemId, ownerId);
        SetUpImpact(problemId, ProblemDeletionImpactDto.Unused);

        Result<ProblemDeletionImpactDto> result = await _handler.Handle(
            new GetStudioProblemDeletionImpactQuery(problemId, ownerId),
            default
        );

        Assert.False(result.Value.IsInPlay);
        Assert.False(result.Value.HasHistory);
    }
}