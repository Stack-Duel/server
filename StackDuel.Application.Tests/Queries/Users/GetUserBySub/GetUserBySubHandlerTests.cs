using Ardalis.Result;
using NSubstitute;
using StackDuel.Application.Queries.Users.GetUserBySub;
using StackDuel.Domain.User;
using StackDuel.Domain.User.Entities;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Application.Tests.Queries.Users.GetUserBySub;

public class GetUserBySubHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetUserBySubHandler _sut;

    public GetUserBySubHandlerTests()
    {
        _sut = new GetUserBySubHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_UserFound_ReturnsSuccessWithMappedDto()
    {
        var user = new User(new Username("valid_user"), new UserSub("auth0|123"), tenant: "acme");
        _userRepository
            .FindBySubAsync(Arg.Is<UserSub>(s => s.Value == "auth0|123"), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new GetUserBySubQuery("auth0|123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.Username.Value, result.Value.Username);
        Assert.Equal(user.Sub.Value, result.Value.Sub);
        Assert.Equal(user.Tenant, result.Value.Tenant);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository
            .FindBySubAsync(Arg.Any<UserSub>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(new GetUserBySubQuery("auth0|123"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}