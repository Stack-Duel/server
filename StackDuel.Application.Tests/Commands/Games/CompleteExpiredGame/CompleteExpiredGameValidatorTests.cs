using FluentValidation.Results;
using CompleteExpiredGameCommand = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameCommand;
using CompleteExpiredGameValidator = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.CompleteExpiredGame;

public class CompleteExpiredGameValidatorTests
{
    private CompleteExpiredGameValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CompleteExpiredGameValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CompleteExpiredGameCommand(Guid.NewGuid(), DateTime.UtcNow, 0);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new CompleteExpiredGameCommand(Guid.Empty, DateTime.UtcNow, 0);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_DefaultExpectedStartedAt_IsInvalid()
    {
        var command = new CompleteExpiredGameCommand(Guid.NewGuid(), default, 0);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_NegativeRescheduleCount_IsInvalid()
    {
        var command = new CompleteExpiredGameCommand(Guid.NewGuid(), DateTime.UtcNow, -1);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_ZeroRescheduleCount_IsValid()
    {
        var command = new CompleteExpiredGameCommand(Guid.NewGuid(), DateTime.UtcNow, 0);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }
}