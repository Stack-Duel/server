using StackDuel.Application.Commands.Users.CreateUser;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Users.CreateUser;

public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateUserCommand("valid_user", "auth0|123");

        var result = _sut.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyUsername_HasError()
    {
        var command = new CreateUserCommand("", "auth0|123");

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Username));
    }

    [Fact]
    public void Validate_UsernameExceedsMaxLength_HasError()
    {
        var command = new CreateUserCommand(new string('a', Username.MaxLength + 1), "auth0|123");

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Username));
    }

    [Fact]
    public void Validate_EmptySub_HasError()
    {
        var command = new CreateUserCommand("valid_user", "");

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Sub));
    }

    [Fact]
    public void Validate_NullImageUrl_HasNoImageUrlError()
    {
        var command = new CreateUserCommand("valid_user", "auth0|123", ImageUrl: null);

        var result = _sut.Validate(command);

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.ImageUrl));
    }

    [Fact]
    public void Validate_ImageUrlExceedsMaxLength_HasError()
    {
        var tooLongUrl = "https://example.com/" + new string('a', ImageUrl.MaxLength);
        var command = new CreateUserCommand("valid_user", "auth0|123", ImageUrl: tooLongUrl);

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.ImageUrl));
    }
}
