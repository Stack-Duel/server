using Ardalis.Result;
using Moq;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Problems.Enums;
using GetMyProblemsMetadataHandler = StackDuel.Application.Queries.Problems.GetMyProblemsMetadata.GetMyProblemsMetadataHandler;
using GetMyProblemsMetadataQuery = StackDuel.Application.Queries.Problems.GetMyProblemsMetadata.GetMyProblemsMetadataQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetMyProblemsMetadata;

public class GetMyProblemsMetadataHandlerTests
{
    private readonly Mock<ISubmissionReadRepository> _submissionReadRepository = new();
    private readonly Mock<IProblemReactionReadRepository> _problemReactionReadRepository = new();
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly UserContext _userContext = new() { User = CreateUserDto() };
    private readonly GetMyProblemsMetadataHandler _handler;

    public GetMyProblemsMetadataHandlerTests()
    {
        _handler = new GetMyProblemsMetadataHandler(
            _submissionReadRepository.Object,
            _problemReactionReadRepository.Object,
            _problemReadRepository.Object,
            _userContext
        );

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(0, 0));

        _problemReactionReadRepository
            .Setup(x => x.GetReactionBreakdownForCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _problemReadRepository
            .Setup(x => x.GetStatusBreakdownForCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static UserDto CreateUserDto() =>
        new(Guid.NewGuid(), "auth0|abc", "alice", null, null, false, null, DateTime.UtcNow, null, []);

    [Fact]
    public async Task Handle_ScopesBothRepositoriesToCurrentUser()
    {
        await _handler.Handle(new GetMyProblemsMetadataQuery(), CancellationToken.None);

        _submissionReadRepository.Verify(
            x => x.GetSubmissionStatsForCreatorAsync(_userContext.User!.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _problemReactionReadRepository.Verify(
            x => x.GetReactionBreakdownForCreatorAsync(_userContext.User!.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _problemReadRepository.Verify(
            x => x.GetStatusBreakdownForCreatorAsync(_userContext.User!.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_OrdersStatusBreakdownAlphabetically()
    {
        ProblemStatusCountDto[] breakdown =
        [
            new(ProblemStatus.Published, 2),
            new(ProblemStatus.Archived, 1),
            new(ProblemStatus.Draft, 5),
        ];
        _problemReadRepository
            .Setup(x => x.GetStatusBreakdownForCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(breakdown);

        Result<MyProblemsMetadataDto> result = await _handler.Handle(
            new GetMyProblemsMetadataQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [ProblemStatus.Archived, ProblemStatus.Draft, ProblemStatus.Published],
            result.Value.StatusBreakdown.Select(x => x.Status)
        );
    }

    [Fact]
    public async Task Handle_ComputesAverageCompletionRateFromSubmissionStats()
    {
        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(3, 2));

        Result<MyProblemsMetadataDto> result = await _handler.Handle(
            new GetMyProblemsMetadataQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalSubmissions);
        Assert.Equal(2.0 / 3.0, result.Value.AverageCompletionRate, 5);
    }

    [Fact]
    public async Task Handle_NoSubmissions_AverageCompletionRateIsZero()
    {
        Result<MyProblemsMetadataDto> result = await _handler.Handle(
            new GetMyProblemsMetadataQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalSubmissions);
        Assert.Equal(0, result.Value.AverageCompletionRate);
    }

    [Fact]
    public async Task Handle_SumsTotalReactionsFromBreakdown()
    {
        ProblemReactionCountDto[] breakdown =
        [
            new("like", "Like", "\U0001F44D", 4),
            new("dislike", "Dislike", "\U0001F44E", 1),
        ];
        _problemReactionReadRepository
            .Setup(x => x.GetReactionBreakdownForCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(breakdown);

        Result<MyProblemsMetadataDto> result = await _handler.Handle(
            new GetMyProblemsMetadataQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalReactions);
        Assert.Equal(breakdown, result.Value.ReactionBreakdown);
    }
}
