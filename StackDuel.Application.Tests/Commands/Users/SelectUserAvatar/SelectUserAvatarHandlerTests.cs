using Ardalis.Result;
using Moq;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Factories;
using StackDuel.Domain.Users.ValueObjects;
using SelectUserAvatarCommand = StackDuel.Application.Commands.Users.SelectUserAvatar.SelectUserAvatarCommand;
using SelectUserAvatarHandler = StackDuel.Application.Commands.Users.SelectUserAvatar.SelectUserAvatarHandler;
using SelectUserAvatarValidator = StackDuel.Application.Commands.Users.SelectUserAvatar.SelectUserAvatarValidator;

namespace StackDuel.Application.Tests.Commands.Users.SelectUserAvatar;

public class SelectUserAvatarHandlerTests
{
    private Mock<IUserWriteRepository> _userRepository = null!;
    private Mock<IUserAvatarWriteRepository> _userAvatarRepository = null!;
    private SelectUserAvatarHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserWriteRepository>();
        _userAvatarRepository = new Mock<IUserAvatarWriteRepository>();

        _handler = new SelectUserAvatarHandler(
            new SelectUserAvatarValidator(),
            _userRepository.Object,
            _userAvatarRepository.Object
        );
    }

    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new SelectUserAvatarCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_AvatarNotOwnedByUser_ReturnsNotFound()
    {
        var user = new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userAvatarRepository
            .Setup(x => x.FindByIdForUserAsync(It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAvatar?)null);

        var command = new SelectUserAvatarCommand(user.Id, Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_AvatarOwnedByUser_SetsImageUrlAndPersists()
    {
        var user = new UserFactory().Create(
            new CreateUserParams("alice", "auth0|abc", "https://storage.example.com/avatars/current.png")
        );
        var avatar = new UserAvatar(user.Id, new ImageUrl("https://storage.example.com/avatars/older.png"));

        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userAvatarRepository
            .Setup(x => x.FindByIdForUserAsync(avatar.Id, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(avatar);

        var command = new SelectUserAvatarCommand(user.Id, avatar.Id);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(user.ImageUrl!.Value, Is.EqualTo("https://storage.example.com/avatars/older.png"));
        });
        _userRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}