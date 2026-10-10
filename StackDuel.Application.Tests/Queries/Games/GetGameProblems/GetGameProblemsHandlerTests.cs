using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using GetGameProblemsHandler = StackDuel.Application.Queries.Games.GetGameProblems.GetGameProblemsHandler;
using GetGameProblemsQuery = StackDuel.Application.Queries.Games.GetGameProblems.GetGameProblemsQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameProblems;

public class GetGameProblemsHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private GetGameProblemsHandler _handler = null!;

    public GetGameProblemsHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _handler = new GetGameProblemsHandler(_gameReadRepository.Object);
    }

    [Fact]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var query = new GetGameProblemsQuery(Guid.NewGuid(), Guid.NewGuid());
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_RequestedByNonParticipant_ReturnsForbidden()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameProblemsQuery(game.Id, Guid.NewGuid());
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_ParticipantWithoutProblemSession_ReturnsEmptyHistories()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameProblemsQuery(game.Id, participantId);
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        GameProblemHistoryDto history = result.Value.Single();
        Assert.Equal(participantId, history.UserId);
        Assert.Empty(history.SolvedProblemIds);
        Assert.Empty(history.SolvedProblemSubmissions);
    }

    [Fact]
    public async Task Handle_ParticipantWithProblemSession_ReturnsSolvedProblemsAndSubmissions()
    {
        var requesterId = Guid.NewGuid();
        var otherParticipantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [requesterId, otherParticipantId], 300);

        var initialProblemId = Guid.NewGuid();
        var solvedProblemId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        GameParticipant requester = game.Participants.Single(p => p.UserId == requesterId);
        requester.InitializeProblemSession(initialProblemId);
        requester.ProblemSession!.AddSolvedProblem(solvedProblemId);
        requester.ProblemSession!.RecordSolvedSubmission(solvedProblemId, submissionId);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameProblemsQuery(game.Id, requesterId);
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        GameProblemHistoryDto history = result.Value.Single(h => h.UserId == requesterId);
        Assert.Equal(new[] { solvedProblemId }, history.SolvedProblemIds);
        Assert.Equal(solvedProblemId, history.SolvedProblemSubmissions.Single().ProblemId);
        Assert.Equal(submissionId, history.SolvedProblemSubmissions.Single().SubmissionId);
        Assert.Equal(2, result.Value.Count);
    }
}