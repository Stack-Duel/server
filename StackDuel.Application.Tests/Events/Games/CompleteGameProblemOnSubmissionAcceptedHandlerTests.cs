using Ardalis.Result;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Events.Games;
using StackDuel.Application.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;

namespace StackDuel.Application.Tests.Events.Games;

public class CompleteGameProblemOnSubmissionAcceptedHandlerTests
{
    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly Mock<IGameProblemCompleter> _gameProblemCompleter = new();
    private readonly CompleteGameProblemOnSubmissionAcceptedHandler _handler;

    public CompleteGameProblemOnSubmissionAcceptedHandlerTests()
    {
        _handler = new CompleteGameProblemOnSubmissionAcceptedHandler(
            _gameReadRepository.Object,
            _gameProblemCompleter.Object,
            NullLogger<CompleteGameProblemOnSubmissionAcceptedHandler>.Instance
        );
    }

    private static Game CreateRunningGameWithParticipantOnAProblem(Guid userId, Guid problemId)
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        game.AppendRound(problemId);
        game.StartRoundFor(userId, problemId);
        return game;
    }

    private Task Handle(SubmissionCompletedDomainEvent domainEvent) =>
        _handler.Handle(
            new DomainEventNotification<SubmissionCompletedDomainEvent>(domainEvent),
            CancellationToken.None
        );

    [Fact]
    public async Task Handle_AcceptedGameSubmission_CompletesTheParticipantsCurrentProblem()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        Game game = CreateRunningGameWithParticipantOnAProblem(userId, problemId);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameProblemCompleter
            .Setup(x => x.CompleteAsync(game.Id, problemId, submissionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GameProblemCompletionDto>.Success(new GameProblemCompletionDto(1, Guid.NewGuid())));

        await Handle(new SubmissionCompletedDomainEvent(userId, submissionId, SubmissionStatus.Accepted, game.Id));

        _gameProblemCompleter.Verify(
            x => x.CompleteAsync(game.Id, problemId, submissionId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_SubmissionOutsideAGame_DoesNothing()
    {
        await Handle(
            new SubmissionCompletedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), SubmissionStatus.Accepted, GameId: null)
        );

        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        VerifyNoCompletion();
    }

    [Fact]
    public async Task Handle_RejectedSubmission_LeavesTheParticipantOnTheSameProblem()
    {
        await Handle(
            new SubmissionCompletedDomainEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                SubmissionStatus.WrongAnswer,
                Guid.NewGuid()
            )
        );

        VerifyNoCompletion();
    }

    [Fact]
    public async Task Handle_ParticipantNotOnARound_DoesNotComplete()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        await Handle(new SubmissionCompletedDomainEvent(userId, Guid.NewGuid(), SubmissionStatus.Accepted, game.Id));

        VerifyNoCompletion();
    }

    [Fact]
    public async Task Handle_GameNoLongerExists_DoesNotComplete()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        await Handle(
            new SubmissionCompletedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), SubmissionStatus.Accepted, gameId)
        );

        VerifyNoCompletion();
    }

    [Fact]
    public async Task Handle_CompletionRejected_SwallowsTheFailure()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGameWithParticipantOnAProblem(userId, problemId);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameProblemCompleter
            .Setup(x =>
                x.CompleteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<GameProblemCompletionDto>.Error("boom"));

        await Handle(new SubmissionCompletedDomainEvent(userId, Guid.NewGuid(), SubmissionStatus.Accepted, game.Id));
    }

    private void VerifyNoCompletion() =>
        _gameProblemCompleter.Verify(
            x =>
                x.CompleteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
}