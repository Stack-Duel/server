using FluentValidation.Results;
using ForfeitGameCommand = StackDuel.Application.Commands.Games.ForfeitGame.ForfeitGameCommand;
using ForfeitGameValidator = StackDuel.Application.Commands.Games.ForfeitGame.ForfeitGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.ForfeitGame;

public class ForfeitGameValidatorTests
{
    private ForfeitGameValidator _validator = null!;

    public ForfeitGameValidatorTests()
    {
        _validator = new ForfeitGameValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new ForfeitGameCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new ForfeitGameCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new ForfeitGameCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}