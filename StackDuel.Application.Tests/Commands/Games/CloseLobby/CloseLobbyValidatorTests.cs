using FluentValidation.Results;
using CloseLobbyCommand = StackDuel.Application.Commands.Games.CloseLobby.CloseLobbyCommand;
using CloseLobbyValidator = StackDuel.Application.Commands.Games.CloseLobby.CloseLobbyValidator;

namespace StackDuel.Application.Tests.Commands.Games.CloseLobby;

public class CloseLobbyValidatorTests
{
    private CloseLobbyValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CloseLobbyValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CloseLobbyCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new CloseLobbyCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new CloseLobbyCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}