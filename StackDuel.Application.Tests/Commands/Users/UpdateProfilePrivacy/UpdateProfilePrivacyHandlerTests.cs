using Ardalis.Result;
using Moq;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Factories;
using UpdateProfilePrivacyCommand = StackDuel.Application.Commands.Users.UpdateProfilePrivacy.UpdateProfilePrivacyCommand;
using UpdateProfilePrivacyHandler = StackDuel.Application.Commands.Users.UpdateProfilePrivacy.UpdateProfilePrivacyHandler;
using UpdateProfilePrivacyValidator = StackDuel.Application.Commands.Users.UpdateProfilePrivacy.UpdateProfilePrivacyValidator;

namespace StackDuel.Application.Tests.Commands.Users.UpdateProfilePrivacy;

public class UpdateProfilePrivacyHandlerTests
{
    private Mock<IUserWriteRepository> _userRepository = null!;
    private UpdateProfilePrivacyHandler _handler = null!;

    public UpdateProfilePrivacyHandlerTests()
    {
        _userRepository = new Mock<IUserWriteRepository>();
        _handler = new UpdateProfilePrivacyHandler(new UpdateProfilePrivacyValidator(), _userRepository.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new UpdateProfilePrivacyCommand(Guid.NewGuid(), true);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UserFound_SetsPrivacyAndPersists()
    {
        var user = new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new UpdateProfilePrivacyCommand(user.Id, true);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsPrivate);
        _userRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserFound_CanUnsetPrivacy()
    {
        var user = new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));
        user.SetPrivate(true);
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new UpdateProfilePrivacyCommand(user.Id, false);
        await _handler.Handle(command, CancellationToken.None);

        Assert.False(user.IsPrivate);
    }
}