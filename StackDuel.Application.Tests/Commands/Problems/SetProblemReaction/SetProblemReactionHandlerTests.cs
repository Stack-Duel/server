using Ardalis.Result;
using Moq;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using SetProblemReactionCommand = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionCommand;
using SetProblemReactionHandler = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionHandler;
using SetProblemReactionValidator = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemReaction;

public class SetProblemReactionHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<IProblemReactionReadRepository> _problemReactionReadRepository = null!;
    private Mock<IProblemReactionWriteRepository> _problemReactionWriteRepository = null!;
    private SetProblemReactionHandler _handler = null!;

    private static readonly ProblemReactionSummaryDto EmptySummary = new([], null);

    public SetProblemReactionHandlerTests()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _problemReactionReadRepository = new Mock<IProblemReactionReadRepository>();
        _problemReactionWriteRepository = new Mock<IProblemReactionWriteRepository>();

        _handler = new SetProblemReactionHandler(
            new SetProblemReactionValidator(),
            _problemReadRepository.Object,
            _problemReactionReadRepository.Object,
            _problemReactionWriteRepository.Object
        );

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptySummary);
    }

    private static SetProblemReactionCommand ValidCommand(string reactionTypeKey = "like") =>
        new(Guid.NewGuid(), Guid.NewGuid(), reactionTypeKey);

    private static ProblemReactionType CreateReactionType(string key) => new(key, key, null, sortOrder: 1);

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepositories()
    {
        var command = new SetProblemReactionCommand(Guid.Empty, Guid.NewGuid(), "like");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemReadRepository.Verify(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UnknownReactionType_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _problemReactionReadRepository
            .Setup(x => x.FindReactionTypeByKeyAsync("bogus", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemReactionType?)null);

        var result = await _handler.Handle(ValidCommand("bogus"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _problemReactionWriteRepository.Verify(
            x =>
                x.FindByProblemUserAndTypeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_NoExistingRow_CreatesActiveReaction()
    {
        var reactionType = CreateReactionType("like");
        _problemReadRepository
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _problemReactionReadRepository
            .Setup(x => x.FindReactionTypeByKeyAsync("like", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reactionType);
        _problemReactionWriteRepository
            .Setup(x =>
                x.FindByProblemUserAndTypeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    reactionType.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ProblemReaction?)null);
        _problemReactionWriteRepository
            .Setup(x =>
                x.FindActiveByProblemAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((ProblemReaction?)null);

        var result = await _handler.Handle(ValidCommand("like"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _problemReactionWriteRepository.Verify(
            x =>
                x.AddAsync(
                    It.Is<ProblemReaction>(r => r.ReactionTypeId == reactionType.Id && r.IsActive),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _problemReactionWriteRepository.Verify(
            x => x.UpdateAsync(It.IsAny<ProblemReaction>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ExistingActiveRowSameKindClickedAgain_Deactivates()
    {
        var reactionType = CreateReactionType("like");
        var existing = new ProblemReaction(Guid.NewGuid(), Guid.NewGuid(), reactionType.Id);

        _problemReadRepository
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _problemReactionReadRepository
            .Setup(x => x.FindReactionTypeByKeyAsync("like", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reactionType);
        _problemReactionWriteRepository
            .Setup(x =>
                x.FindByProblemUserAndTypeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    reactionType.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existing);

        var result = await _handler.Handle(ValidCommand("like"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(existing.IsActive);
        _problemReactionWriteRepository.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _problemReactionWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<ProblemReaction>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _problemReactionWriteRepository.Verify(
            x => x.FindActiveByProblemAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ExistingInactiveRowSameKind_Reactivates()
    {
        var reactionType = CreateReactionType("like");
        var existing = new ProblemReaction(Guid.NewGuid(), Guid.NewGuid(), reactionType.Id);
        existing.Deactivate();

        _problemReadRepository
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _problemReactionReadRepository
            .Setup(x => x.FindReactionTypeByKeyAsync("like", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reactionType);
        _problemReactionWriteRepository
            .Setup(x =>
                x.FindByProblemUserAndTypeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    reactionType.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existing);
        _problemReactionWriteRepository
            .Setup(x =>
                x.FindActiveByProblemAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((ProblemReaction?)null);

        var result = await _handler.Handle(ValidCommand("like"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(existing.IsActive);
        _problemReactionWriteRepository.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _problemReactionWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<ProblemReaction>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ActivatingDifferentKindWhileAnotherIsActive_DeactivatesOldFirst()
    {
        var likeType = CreateReactionType("like");
        var dislikeType = CreateReactionType("dislike");
        var problemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activeDislike = new ProblemReaction(problemId, userId, dislikeType.Id);

        _problemReadRepository
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _problemReactionReadRepository
            .Setup(x => x.FindReactionTypeByKeyAsync("like", It.IsAny<CancellationToken>()))
            .ReturnsAsync(likeType);
        _problemReactionWriteRepository
            .Setup(x => x.FindByProblemUserAndTypeAsync(problemId, userId, likeType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemReaction?)null);
        _problemReactionWriteRepository
            .Setup(x => x.FindActiveByProblemAndUserAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeDislike);

        var command = new SetProblemReactionCommand(problemId, userId, "like");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(activeDislike.IsActive);
        _problemReactionWriteRepository.Verify(
            x => x.UpdateAsync(activeDislike, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _problemReactionWriteRepository.Verify(
            x =>
                x.AddAsync(
                    It.Is<ProblemReaction>(r => r.ReactionTypeId == likeType.Id && r.IsActive),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}