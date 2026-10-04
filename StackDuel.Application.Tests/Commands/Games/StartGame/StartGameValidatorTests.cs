using FluentValidation.Results;
using StartGameCommand = StackDuel.Application.Commands.Games.StartGame.StartGameCommand;
using StartGameValidator = StackDuel.Application.Commands.Games.StartGame.StartGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.StartGame;

public class StartGameValidatorTests
{
    private StartGameValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new StartGameValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new StartGameCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new StartGameCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new StartGameCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}