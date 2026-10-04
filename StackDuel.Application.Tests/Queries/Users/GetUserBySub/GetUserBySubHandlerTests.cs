using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using Ardalis.Result;
using Moq;
using GetUserBySubHandler = StackDuel.Application.Queries.Users.GetUserBySub.GetUserBySubHandler;
using GetUserBySubQuery = StackDuel.Application.Queries.Users.GetUserBySub.GetUserBySubQuery;

namespace StackDuel.Application.Tests.Queries.Users.GetUserBySub;

public class GetUserBySubHandlerTests
{
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetUserBySubHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userReadRepository = new Mock<IUserReadRepository>();
        _handler = new GetUserBySubHandler(_userReadRepository.Object);
    }

    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userReadRepository
            .Setup(x => x.FindBySubAsync("auth0|missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        Result<UserDto> result = await _handler.Handle(new GetUserBySubQuery("auth0|missing"), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_UserFound_ReturnsUser()
    {
        var user = new UserDto(
            Guid.NewGuid(),
            "auth0|abc",
            "alice",
            null,
            null,
            false,
            null,
            DateTime.UtcNow,
            null,
            []
        );
        _userReadRepository.Setup(x => x.FindBySubAsync("auth0|abc", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Result<UserDto> result = await _handler.Handle(new GetUserBySubQuery("auth0|abc"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.SameAs(user));
    }
}