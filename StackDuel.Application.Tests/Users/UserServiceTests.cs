using Ardalis.Result;
using Mediator;
using NSubstitute;
using StackDuel.Application.Commands.Users.CreateUser;
using StackDuel.Application.Queries.Users.GetUserBySub;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Tests.Users;

public class UserServiceTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_sender);
    }

    [Fact]
    public async Task CreateAsync_SendsCreateUserCommandWithProvidedValues()
    {
        var expected = Result.Success(Guid.NewGuid());
        _sender
            .Send(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(expected));

        var result = await _sut.CreateAsync("valid_user", "auth0|123", "https://example.com/a.png", "acme", CancellationToken.None);

        Assert.Equal(expected, result);
        await _sender
            .Received(1)
            .Send(
                Arg.Is<CreateUserCommand>(c =>
                    c.Username == "valid_user"
                    && c.Sub == "auth0|123"
                    && c.ImageUrl == "https://example.com/a.png"
                    && c.Tenant == "acme"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetBySubAsync_SendsGetUserBySubQueryWithProvidedSub()
    {
        var expected = Result.Success(new UserDto(Guid.NewGuid(), "valid_user", "auth0|123", null, null, null));
        _sender
            .Send(Arg.Any<GetUserBySubQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(expected));

        var result = await _sut.GetBySubAsync("auth0|123", CancellationToken.None);

        Assert.Equal(expected, result);
        await _sender
            .Received(1)
            .Send(Arg.Is<GetUserBySubQuery>(q => q.Sub == "auth0|123"), Arg.Any<CancellationToken>());
    }
}
