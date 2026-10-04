using StackDuel.Application.Events;
using StackDuel.Application.Groups;
using StackDuel.Application.Groups.Dtos;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Events;
using StackDuel.Domain.Users.Factories;
using Ardalis.Result;
using Moq;
using UpdateUserGroupsCommand = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsCommand;
using UpdateUserGroupsHandler = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsHandler;
using UpdateUserGroupsValidator = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsValidator;

namespace StackDuel.Application.Tests.Commands.Users.UpdateUserGroups;

public class UpdateUserGroupsHandlerTests
{
    private Mock<IUserWriteRepository> _userRepository = null!;
    private Mock<IGroupReadRepository> _groupRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private UserContext _userContext = null!;
    private UpdateUserGroupsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserWriteRepository>();
        _groupRepository = new Mock<IGroupReadRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _userContext = new UserContext();
        _handler = new UpdateUserGroupsHandler(
            new UpdateUserGroupsValidator(),
            _userRepository.Object,
            _groupRepository.Object,
            _domainEventDispatcher.Object,
            _userContext
        );
    }

    private static User CreateUser() => new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Sub, user.Username.Value, null, null, false, null, user.CreatedAt, null, []);

    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), [Guid.NewGuid()]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_UnknownGroupId_ReturnsInvalidAndDoesNotSetGroups()
    {
        var user = CreateUser();
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _groupRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupDto(Guid.NewGuid(), "Admins")]);

        var command = new UpdateUserGroupsCommand(user.Id, [Guid.NewGuid()]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _userRepository.Verify(
            x =>
                x.SetGroupsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_AllGroupIdsValid_SetsGroupsAndSucceeds()
    {
        var user = CreateUser();
        var groupId = Guid.NewGuid();
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _groupRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupDto(groupId, "Admins")]);

        var command = new UpdateUserGroupsCommand(user.Id, [groupId]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Verify(
            x =>
                x.SetGroupsAsync(
                    user.Id,
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == groupId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _domainEventDispatcher.Verify(
            x =>
                x.DispatchAsync(
                    It.Is<IEnumerable<IDomainEvent>>(events =>
                        events.OfType<UserAccessContextChangedDomainEvent>().Any(e => e.Sub == user.Sub)
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_EmptyGroupIds_SetsEmptyGroupsAndSucceeds()
    {
        var user = CreateUser();
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _groupRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupDto(Guid.NewGuid(), "Admins")]);

        var command = new UpdateUserGroupsCommand(user.Id, []);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Verify(
            x =>
                x.SetGroupsAsync(
                    user.Id,
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_RemovingAdminGroupFromAnotherAdmin_ReturnsForbiddenAndDoesNotSetGroups()
    {
        var actingAdmin = CreateUser();
        var targetAdmin = CreateUser();
        var adminGroupId = Guid.NewGuid();
        _userContext.User = ToDto(actingAdmin);

        _userRepository
            .Setup(x => x.FindByIdAsync(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);
        _groupRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupDto(adminGroupId, WellKnownAuthorization.AdminGroup)]);
        _userRepository
            .Setup(x => x.GetGroupIdsAsync(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([adminGroupId]);

        var command = new UpdateUserGroupsCommand(targetAdmin.Id, []);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Forbidden));
        _userRepository.Verify(
            x =>
                x.SetGroupsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_AdminRemovingOwnAdminGroup_SetsGroupsAndSucceeds()
    {
        var actingAdmin = CreateUser();
        var adminGroupId = Guid.NewGuid();
        _userContext.User = ToDto(actingAdmin);

        _userRepository
            .Setup(x => x.FindByIdAsync(actingAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actingAdmin);
        _groupRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupDto(adminGroupId, WellKnownAuthorization.AdminGroup)]);
        _userRepository
            .Setup(x => x.GetGroupIdsAsync(actingAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([adminGroupId]);

        var command = new UpdateUserGroupsCommand(actingAdmin.Id, []);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Verify(
            x =>
                x.SetGroupsAsync(
                    actingAdmin.Id,
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_GrantingAdminGroupToNonAdmin_SetsGroupsAndSucceeds()
    {
        var actingAdmin = CreateUser();
        var targetUser = CreateUser();
        var adminGroupId = Guid.NewGuid();
        _userContext.User = ToDto(actingAdmin);

        _userRepository
            .Setup(x => x.FindByIdAsync(targetUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);
        _groupRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupDto(adminGroupId, WellKnownAuthorization.AdminGroup)]);
        _userRepository.Setup(x => x.GetGroupIdsAsync(targetUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var command = new UpdateUserGroupsCommand(targetUser.Id, [adminGroupId]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Verify(
            x =>
                x.SetGroupsAsync(
                    targetUser.Id,
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == adminGroupId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new UpdateUserGroupsCommand(Guid.Empty, [Guid.NewGuid()]);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _userRepository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}