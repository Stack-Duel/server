using FluentValidation.Results;
using ForfeitGameCommand = StackDuel.Application.Commands.Games.ForfeitGame.ForfeitGameCommand;
using ForfeitGameValidator = StackDuel.Application.Commands.Games.ForfeitGame.ForfeitGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.ForfeitGame;

public class ForfeitGameValidatorTests
{
    private ForfeitGameValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new ForfeitGameValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new ForfeitGameCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new ForfeitGameCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new ForfeitGameCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}