using Ardalis.Result;
using Moq;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos.Admin;
using GetAdminUsersPageableHandler = StackDuel.Application.Queries.Users.GetAdminUsersPageable.GetAdminUsersPageableHandler;
using GetAdminUsersPageableQuery = StackDuel.Application.Queries.Users.GetAdminUsersPageable.GetAdminUsersPageableQuery;

namespace StackDuel.Application.Tests.Queries.Users.GetAdminUsersPageable;

public class GetAdminUsersPageableHandlerTests
{
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetAdminUsersPageableHandler _handler = null!;

    public GetAdminUsersPageableHandlerTests()
    {
        _userReadRepository = new Mock<IUserReadRepository>();
        _handler = new GetAdminUsersPageableHandler(_userReadRepository.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResultFromRepository()
    {
        var pagination = new PaginationRequest { Page = 1, Size = 50 };
        var expected = new PageResult<AdminUserDto>
        {
            Results = [new AdminUserDto(Guid.NewGuid(), "alice", null, null, DateTime.UtcNow, null, [])],
            Total = 1,
            Page = 1,
            Size = 50,
        };

        _userReadRepository
            .Setup(x => x.GetAdminUsersPageableAsync(pagination, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        Result<PageResult<AdminUserDto>> result = await _handler.Handle(
            new GetAdminUsersPageableQuery(pagination, null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }
}