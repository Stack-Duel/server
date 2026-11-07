using Ardalis.Result;
using Moq;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Tests.Queries.StudioProblems.GetStudioProblemById;

public class GetStudioProblemByIdHandlerTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly Mock<IProblemReactionReadRepository> _problemReactionReadRepository = new();
    private readonly Mock<ISubmissionReadRepository> _submissionReadRepository = new();
    private readonly GetStudioProblemByIdHandler _handler;

    public GetStudioProblemByIdHandlerTests()
    {
        _handler = new GetStudioProblemByIdHandler(
            _problemReadRepository.Object,
            _problemReactionReadRepository.Object,
            _submissionReadRepository.Object
        );
    }

    private static StudioProblemDetailsDto CreateCoreDetails(Guid problemId) =>
        new(
            problemId,
            "two-sum",
            "Two Sum",
            DifficultyValue: 2,
            ProblemStatus.Published,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            SetupCount: 3,
            CreatedAt: DateTime.UtcNow
        );

    [Fact]
    public async Task Handle_ProblemExists_ReturnsDetailsWithReactionAndSolveStats()
    {
        Guid problemId = Guid.NewGuid();
        StudioProblemDetailsDto core = CreateCoreDetails(problemId);

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(core);

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(problemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ProblemReactionSummaryDto(
                    [new ProblemReactionCountDto("like", "Like", "👍", 7), new ProblemReactionCountDto("love", "Love", "❤️", 3)],
                    null
                )
            );

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForProblemAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(TotalSubmissions: 40, AcceptedSubmissions: 25));

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.ReactionCount);
        Assert.Equal(40, result.Value.AttemptCount);
        Assert.Equal(62.5, result.Value.SolveRatePercent);
        Assert.Equal(DifficultyTier.Beginner, result.Value.DifficultyTier);
    }

    [Fact]
    public async Task Handle_NoAttempts_SolveRateIsZero()
    {
        Guid problemId = Guid.NewGuid();
        StudioProblemDetailsDto core = CreateCoreDetails(problemId);

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(core);

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(problemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemReactionSummaryDto([], null));

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForProblemAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(TotalSubmissions: 0, AcceptedSubmissions: 0));

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(0, result.Value.SolveRatePercent);
    }

    [Fact]
    public async Task Handle_ProblemMissing_ReturnsNotFound()
    {
        Guid problemId = Guid.NewGuid();

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudioProblemDetailsDto?)null);

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _problemReactionReadRepository.VerifyNoOtherCalls();
        _submissionReadRepository.VerifyNoOtherCalls();
    }
}
