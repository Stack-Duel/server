using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Authorization;
using Ardalis.Result;
using Moq;
using GetUserAccessContextHandler = StackDuel.Application.Queries.Permissions.GetUserAccessContext.GetUserAccessContextHandler;
using GetUserAccessContextQuery = StackDuel.Application.Queries.Permissions.GetUserAccessContext.GetUserAccessContextQuery;

namespace StackDuel.Application.Tests.Queries.Permissions.GetUserAccessContext;

public class GetUserAccessContextHandlerTests
{
    private const string ValidSub = "auth0|abc123";

    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<IAuthorizationReadRepository> _authorizationReadRepository = null!;
    private GetUserAccessContextHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userReadRepository = new Mock<IUserReadRepository>();
        _authorizationReadRepository = new Mock<IAuthorizationReadRepository>();
        _handler = new GetUserAccessContextHandler(_userReadRepository.Object, _authorizationReadRepository.Object);
    }

    private static UserDto CreateUser() =>
        new(Guid.NewGuid(), ValidSub, "alice", null, null, false, null, DateTime.UtcNow, DateTime.UtcNow, []);

    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userReadRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        Result<UserAccessContextDto> result = await _handler.Handle(
            new GetUserAccessContextQuery(ValidSub),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
        _authorizationReadRepository.Verify(
            x => x.GetUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_UserFound_ComposesAccessContextFromPermissionsAndRoles()
    {
        var user = CreateUser();
        _userReadRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _authorizationReadRepository
            .Setup(x => x.GetUserPermissionsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["problems:read", "problems:write"]);
        _authorizationReadRepository
            .Setup(x => x.GetUserRoleNamesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Admin"]);

        Result<UserAccessContextDto> result = await _handler.Handle(
            new GetUserAccessContextQuery(ValidSub),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.User, Is.EqualTo(user));
            Assert.That(result.Value.Permissions, Is.EquivalentTo(new[] { "problems:read", "problems:write" }));
            Assert.That(result.Value.Roles, Is.EquivalentTo(new[] { "Admin" }));
        });
    }

    [Test]
    public async Task Handle_UserFound_LooksUpPermissionsAndRolesByUserId()
    {
        var user = CreateUser();
        _userReadRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _authorizationReadRepository
            .Setup(x => x.GetUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _authorizationReadRepository
            .Setup(x => x.GetUserRoleNamesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _handler.Handle(new GetUserAccessContextQuery(ValidSub), CancellationToken.None);

        _authorizationReadRepository.Verify(
            x => x.GetUserPermissionsAsync(user.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _authorizationReadRepository.Verify(
            x => x.GetUserRoleNamesAsync(user.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}