using Ardalis.Result;
using NSubstitute;
using StackDuel.Application.Commands.Users.CreateUser;
using StackDuel.Domain.User;
using StackDuel.Domain.User.Entities;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Users.CreateUser;

public class CreateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly CreateUserHandler _sut;

    public CreateUserHandlerTests()
    {
        _sut = new CreateUserHandler(new CreateUserValidator(), _userRepository);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidWithoutCheckingRepository()
    {
        var command = new CreateUserCommand("", "auth0|123");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        await _userRepository.DidNotReceive().FindBySubAsync(Arg.Any<UserSub>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubAlreadyExists_ReturnsErrorWithoutAddingUser()
    {
        var command = new CreateUserCommand("valid_user", "auth0|123");
        var existingUser = new User(new Username("existing_user"), new UserSub(command.Sub));
        _userRepository.FindBySubAsync(Arg.Any<UserSub>(), Arg.Any<CancellationToken>()).Returns(existingUser);

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Error, result.Status);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewUser_AddsUserAndReturnsItsId()
    {
        var command = new CreateUserCommand("valid_user", "auth0|123");
        _userRepository
            .FindBySubAsync(Arg.Any<UserSub>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _userRepository
            .Received(1)
            .AddAsync(
                Arg.Is<User>(u => u.Username.Value == command.Username && u.Sub.Value == command.Sub),
                Arg.Any<CancellationToken>()
            );
    }
}
