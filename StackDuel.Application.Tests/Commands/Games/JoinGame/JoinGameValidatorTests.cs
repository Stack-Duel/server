using FluentValidation.Results;
using JoinGameCommand = StackDuel.Application.Commands.Games.JoinGame.JoinGameCommand;
using JoinGameValidator = StackDuel.Application.Commands.Games.JoinGame.JoinGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.JoinGame;

public class JoinGameValidatorTests
{
    private JoinGameValidator _validator = null!;

    public JoinGameValidatorTests()
    {
        _validator = new JoinGameValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new JoinGameCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new JoinGameCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}