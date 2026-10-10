using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Submissions.Enums;
using GetAdminGamePlayerHistoryHandler = StackDuel.Application.Queries.Games.GetAdminGamePlayerHistory.GetAdminGamePlayerHistoryHandler;
using GetAdminGamePlayerHistoryQuery = StackDuel.Application.Queries.Games.GetAdminGamePlayerHistory.GetAdminGamePlayerHistoryQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetAdminGamePlayerHistory;

public class GetAdminGamePlayerHistoryHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private GetAdminGamePlayerHistoryHandler _handler = null!;

    public GetAdminGamePlayerHistoryHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _handler = new GetAdminGamePlayerHistoryHandler(
            _gameReadRepository.Object,
            _submissionReadRepository.Object,
            _problemReadRepository.Object
        );
    }

    private static AdminProblemListRowDto MakeProblemRow(Guid id, string title, string slug) =>
        new(id, slug, title, 1, ProblemStatus.Published, 1000, 256, [], [], 1, DateTime.UtcNow, null);

    [Fact]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(r => r.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var result = await _handler.Handle(new GetAdminGamePlayerHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ParticipantWithSolvedAndWrongSubmissions_OrdersEventsChronologically()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);
        var problemId = Guid.NewGuid();

        var wrongSubmissionId = Guid.NewGuid();
        var acceptedSubmissionId = Guid.NewGuid();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _gameReadRepository.Setup(r => r.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionReadRepository
            .Setup(r => r.GetSubmissionsForGameAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new GameSubmissionEventDto(
                    wrongSubmissionId,
                    participantId,
                    problemId,
                    "Two Sum",
                    "two-sum",
                    SubmissionStatus.WrongAnswer,
                    baseTime,
                    "Python"
                ),
                new GameSubmissionEventDto(
                    acceptedSubmissionId,
                    participantId,
                    problemId,
                    "Two Sum",
                    "two-sum",
                    SubmissionStatus.Accepted,
                    baseTime.AddMinutes(2),
                    "Python"
                ),
            ]);

        var result = await _handler.Handle(new GetAdminGamePlayerHistoryQuery(game.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var history = result.Value.Single(h => h.UserId == participantId);
        Assert.Equal(2, history.Events.Count);
        Assert.Equal(AdminGameHistoryEventType.WrongAnswer, history.Events[0].Type);
        Assert.Equal(wrongSubmissionId, history.Events[0].SubmissionId);
        Assert.Equal(AdminGameHistoryEventType.Accepted, history.Events[1].Type);
        Assert.Equal(acceptedSubmissionId, history.Events[1].SubmissionId);
    }

    [Fact]
    public async Task Handle_SkippedProblemWithNoSubmissions_ResolvesTitleFromProblemRepository()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        var firstProblemId = Guid.NewGuid();
        var skippedProblemId = Guid.NewGuid();

        GameParticipant participant = game.Participants.Single(p => p.UserId == participantId);
        participant.InitializeProblemSession(firstProblemId);
        participant.ProblemSession!.AddSkippedProblem(skippedProblemId);

        _gameReadRepository.Setup(r => r.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionReadRepository
            .Setup(r => r.GetSubmissionsForGameAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _problemReadRepository
            .Setup(r =>
                r.FindByIdsAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(skippedProblemId)),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([MakeProblemRow(skippedProblemId, "Merge Intervals", "merge-intervals")]);

        var result = await _handler.Handle(new GetAdminGamePlayerHistoryQuery(game.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var history = result.Value.Single(h => h.UserId == participantId);
        var skipEvent = history.Events.Single();
        Assert.Equal(AdminGameHistoryEventType.Skipped, skipEvent.Type);
        Assert.Equal(skippedProblemId, skipEvent.ProblemId);
        Assert.Equal("Merge Intervals", skipEvent.ProblemTitle);
        Assert.Null(skipEvent.OccurredAt);
    }

    [Fact]
    public async Task Handle_SkippedProblemWithPriorWrongSubmission_UsesLastSubmissionTimeAsApproxTimestamp()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);
        var skippedProblemId = Guid.NewGuid();
        var lastAttemptAt = new DateTime(2026, 1, 1, 0, 5, 0, DateTimeKind.Utc);

        GameParticipant participant = game.Participants.Single(p => p.UserId == participantId);
        participant.InitializeProblemSession(skippedProblemId);
        participant.ProblemSession!.AddSkippedProblem(skippedProblemId);

        _gameReadRepository.Setup(r => r.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionReadRepository
            .Setup(r => r.GetSubmissionsForGameAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new GameSubmissionEventDto(
                    Guid.NewGuid(),
                    participantId,
                    skippedProblemId,
                    "Merge Intervals",
                    "merge-intervals",
                    SubmissionStatus.WrongAnswer,
                    lastAttemptAt,
                    "Python"
                ),
            ]);
        _problemReadRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeProblemRow(skippedProblemId, "Merge Intervals", "merge-intervals")]);

        var result = await _handler.Handle(new GetAdminGamePlayerHistoryQuery(game.Id), CancellationToken.None);

        var history = result.Value.Single(h => h.UserId == participantId);
        var skipEvent = history.Events.Single(e => e.Type == AdminGameHistoryEventType.Skipped);
        Assert.Equal(lastAttemptAt, skipEvent.OccurredAt);
    }
}