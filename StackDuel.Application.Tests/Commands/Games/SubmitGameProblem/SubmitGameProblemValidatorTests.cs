using FluentValidation.Results;
using StackDuel.Domain.Submissions.ValueObjects;
using SubmitGameProblemCommand = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemCommand;
using SubmitGameProblemValidator = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.SubmitGameProblem;

public class SubmitGameProblemValidatorTests
{
    private SubmitGameProblemValidator _validator = null!;

    private static SubmitGameProblemCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "print('hi')", Guid.NewGuid());

    public SubmitGameProblemValidatorTests()
    {
        _validator = new SubmitGameProblemValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = ValidCommand() with { GameId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyProblemId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyProblemSetupId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemSetupId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCode_IsInvalid()
    {
        var command = ValidCommand() with { Code = string.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = ValidCommand() with { RequestedByUserId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_CodeAtSourceCodeMaxLength_IsValid()
    {
        var command = ValidCommand() with { Code = new string('x', SourceCode.MaxLength) };

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CodeLongerThanSourceCodeMaxLength_IsInvalid()
    {
        var command = ValidCommand() with { Code = new string('x', SourceCode.MaxLength + 1) };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}