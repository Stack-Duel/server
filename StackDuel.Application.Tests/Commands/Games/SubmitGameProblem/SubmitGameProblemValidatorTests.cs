using FluentValidation.Results;
using SubmitGameProblemCommand = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemCommand;
using SubmitGameProblemValidator = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.SubmitGameProblem;

public class SubmitGameProblemValidatorTests
{
    private SubmitGameProblemValidator _validator = null!;

    private static SubmitGameProblemCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "print('hi')", Guid.NewGuid());

    [SetUp]
    public void SetUp()
    {
        _validator = new SubmitGameProblemValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = ValidCommand() with { GameId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyProblemId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyProblemSetupId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemSetupId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyCode_IsInvalid()
    {
        var command = ValidCommand() with { Code = string.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = ValidCommand() with { RequestedByUserId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}