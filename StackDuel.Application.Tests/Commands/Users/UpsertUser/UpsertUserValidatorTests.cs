using FluentValidation.Results;
using UpsertUserCommand = StackDuel.Application.Commands.Users.UpsertUser.UpsertUserCommand;
using UpsertUserValidator = StackDuel.Application.Commands.Users.UpsertUser.UpsertUserValidator;

namespace StackDuel.Application.Tests.Commands.Users.UpsertUser;

public class UpsertUserValidatorTests
{
    private UpsertUserValidator _validator = null!;

    public UpsertUserValidatorTests()
    {
        _validator = new UpsertUserValidator();
    }

    [Fact]
    public void Validate_AllFieldsWithinLimits_IsValid()
    {
        var command = new UpsertUserCommand("auth0|abc", "alice", "https://example.com/a.png", "Hello.");

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UsernameTooLong_IsInvalid()
    {
        var command = new UpsertUserCommand("auth0|abc", new string('a', 21), null, null);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullUsername_IsValid()
    {
        var command = new UpsertUserCommand("auth0|abc", null, null, null);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ImageUrlTooLong_IsInvalid()
    {
        var command = new UpsertUserCommand("auth0|abc", null, new string('a', 2049), null);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_BioTooLong_IsInvalid()
    {
        var command = new UpsertUserCommand("auth0|abc", null, null, new string('a', 501));

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DuplicateLanguageIds_IsInvalid()
    {
        var languageId = Guid.NewGuid();
        var command = new UpsertUserCommand("auth0|abc", null, null, null, [languageId, languageId]);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DistinctLanguageIds_IsValid()
    {
        var command = new UpsertUserCommand("auth0|abc", null, null, null, [Guid.NewGuid(), Guid.NewGuid()]);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullLanguageIds_IsValid()
    {
        var command = new UpsertUserCommand("auth0|abc", null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}