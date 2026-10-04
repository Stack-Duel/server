using FluentValidation.Results;
using LeaveGameCommand = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameCommand;
using LeaveGameValidator = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.LeaveGame;

public class LeaveGameValidatorTests
{
    private LeaveGameValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new LeaveGameValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new LeaveGameCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new LeaveGameCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new LeaveGameCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}