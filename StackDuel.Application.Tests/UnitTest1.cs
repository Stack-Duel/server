using NSubstitute;
using StackDuel.Application.Commands.Users.CreateUser;
using StackDuel.Domain.User;
using StackDuel.Domain.User.ValueObjects;
using User = StackDuel.Domain.User.Entities.User;

namespace StackDuel.Application.Tests;

public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        var result = _validator.Validate(new CreateUserCommand("alice", "sub-1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptySub_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand("alice", ""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UsernameTooLong_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand(new string('a', Username.MaxLength + 1), "sub-1"));

        Assert.False(result.IsValid);
    }
}

public class CreateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _handler = new CreateUserHandler(new CreateUserValidator(), _userRepository);
    }

    [Fact]
    public async Task ExistingSub_ReturnsError()
    {
        _userRepository.FindBySubAsync(Arg.Any<UserSub>(), Arg.Any<CancellationToken>()).Returns(new User(new Username("alice"), new UserSub("sub-1")));

        var result = await _handler.Handle(new CreateUserCommand("alice", "sub-1"), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task NewSub_AddsUserAndReturnsId()
    {
        _userRepository.FindBySubAsync(Arg.Any<UserSub>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(new CreateUserCommand("alice", "sub-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}