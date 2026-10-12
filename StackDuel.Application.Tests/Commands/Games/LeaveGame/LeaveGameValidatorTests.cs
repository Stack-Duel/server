using FluentValidation.Results;
using LeaveGameCommand = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameCommand;
using LeaveGameValidator = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.LeaveGame;

public class LeaveGameValidatorTests
{
    private LeaveGameValidator _validator = null!;

    public LeaveGameValidatorTests()
    {
        _validator = new LeaveGameValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new LeaveGameCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new LeaveGameCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new LeaveGameCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}