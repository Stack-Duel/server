using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos.Admin;
using Ardalis.Result;
using Moq;
using GetAdminUserDetailHandler = StackDuel.Application.Queries.Users.GetAdminUserDetail.GetAdminUserDetailHandler;
using GetAdminUserDetailQuery = StackDuel.Application.Queries.Users.GetAdminUserDetail.GetAdminUserDetailQuery;

namespace StackDuel.Application.Tests.Queries.Users.GetAdminUserDetail;

public class GetAdminUserDetailHandlerTests
{
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetAdminUserDetailHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userReadRepository = new Mock<IUserReadRepository>();
        _handler = new GetAdminUserDetailHandler(_userReadRepository.Object);
    }

    [Test]
    public async Task Handle_ReturnsUserFromRepository()
    {
        Guid userId = Guid.NewGuid();
        var expected = new AdminUserDetailDto(userId, "alice", null, null, false, null, DateTime.UtcNow, null, []);

        _userReadRepository
            .Setup(x => x.FindAdminUserDetailByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        Result<AdminUserDetailDto> result = await _handler.Handle(
            new GetAdminUserDetailQuery(userId),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.SameAs(expected));
    }

    [Test]
    public async Task Handle_ReturnsNotFound_WhenUserDoesNotExist()
    {
        Guid userId = Guid.NewGuid();

        _userReadRepository
            .Setup(x => x.FindAdminUserDetailByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUserDetailDto?)null);

        Result<AdminUserDetailDto> result = await _handler.Handle(
            new GetAdminUserDetailQuery(userId),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }
}