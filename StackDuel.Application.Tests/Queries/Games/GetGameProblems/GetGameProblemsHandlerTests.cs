using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;
using Moq;
using GetGameProblemsHandler = StackDuel.Application.Queries.Games.GetGameProblems.GetGameProblemsHandler;
using GetGameProblemsQuery = StackDuel.Application.Queries.Games.GetGameProblems.GetGameProblemsQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameProblems;

public class GetGameProblemsHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private GetGameProblemsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _handler = new GetGameProblemsHandler(_gameReadRepository.Object);
    }

    [Test]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var query = new GetGameProblemsQuery(Guid.NewGuid(), Guid.NewGuid());
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_RequestedByNonParticipant_ReturnsForbidden()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameProblemsQuery(game.Id, Guid.NewGuid());
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Forbidden));
    }

    [Test]
    public async Task Handle_ParticipantWithoutProblemSession_ReturnsEmptyHistories()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameProblemsQuery(game.Id, participantId);
        Result<IReadOnlyList<GameProblemHistoryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        GameProblemHistoryDto history = result.Value.Single();
        Assert.Multiple(() =>
        {
            Assert.That(history.UserId, Is.EqualTo(participantId));
            Assert.That(history.SolvedProblemIds, Is.Empty);
            Assert.That(history.SolvedProblemSubmissions, Is.Empty);
        });
    }

    [Test]
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

        Assert.That(result.IsSuccess, Is.True);
        GameProblemHistoryDto history = result.Value.Single(h => h.UserId == requesterId);
        Assert.Multiple(() =>
        {
            Assert.That(history.SolvedProblemIds, Is.EqualTo(new[] { solvedProblemId }));
            Assert.That(history.SolvedProblemSubmissions.Single().ProblemId, Is.EqualTo(solvedProblemId));
            Assert.That(history.SolvedProblemSubmissions.Single().SubmissionId, Is.EqualTo(submissionId));
        });
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }
}