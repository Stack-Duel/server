using Ardalis.Result;
using Moq;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Problems;
using StackDuel.Domain.DailyChallenges.Entities;
using UpdateDailyChallengeCommand = StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge.UpdateDailyChallengeCommand;
using UpdateDailyChallengeHandler = StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge.UpdateDailyChallengeHandler;
using UpdateDailyChallengeValidator = StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge.UpdateDailyChallengeValidator;

namespace StackDuel.Application.Tests.Commands.DailyChallenges.UpdateDailyChallenge;

public class UpdateDailyChallengeHandlerTests
{
    private Mock<IDailyChallengeRepository> _dailyChallengeRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private UpdateDailyChallengeHandler _handler = null!;

    public UpdateDailyChallengeHandlerTests()
    {
        _dailyChallengeRepository = new Mock<IDailyChallengeRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _handler = new UpdateDailyChallengeHandler(
            _dailyChallengeRepository.Object,
            _problemReadRepository.Object,
            new UpdateDailyChallengeValidator()
        );
    }

    private static DateOnly Tomorrow() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

    [Fact]
    public async Task Handle_DateIsToday_ReturnsInvalid()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Result result = await _handler.Handle(
            new UpdateDailyChallengeCommand(today, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_DateIsInThePast_ReturnsInvalid()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        Result result = await _handler.Handle(
            new UpdateDailyChallengeCommand(yesterday, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_NoChallengeScheduledForDate_ReturnsNotFound()
    {
        var date = Tomorrow();
        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyChallenge?)null);

        Result result = await _handler.Handle(
            new UpdateDailyChallengeCommand(date, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemDoesNotExist_ReturnsInvalid()
    {
        var date = Tomorrow();
        var challenge = new DailyChallenge(date, Guid.NewGuid());
        var newProblemId = Guid.NewGuid();

        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _problemReadRepository
            .Setup(x => x.ExistsForAdminAsync(newProblemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result result = await _handler.Handle(
            new UpdateDailyChallengeCommand(date, newProblemId),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _dailyChallengeRepository.Verify(
            x => x.UpdateAsync(It.IsAny<DailyChallenge>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesTheChallengesProblem()
    {
        var date = Tomorrow();
        var challenge = new DailyChallenge(date, Guid.NewGuid());
        var newProblemId = Guid.NewGuid();

        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _problemReadRepository
            .Setup(x => x.ExistsForAdminAsync(newProblemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await _handler.Handle(
            new UpdateDailyChallengeCommand(date, newProblemId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(newProblemId, challenge.ProblemId);
        _dailyChallengeRepository.Verify(x => x.UpdateAsync(challenge, It.IsAny<CancellationToken>()), Times.Once);
    }
}