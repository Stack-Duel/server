using FluentValidation.Results;
using CompleteProblemCommand = StackDuel.Application.Commands.Games.CompleteProblem.CompleteProblemCommand;
using CompleteProblemValidator = StackDuel.Application.Commands.Games.CompleteProblem.CompleteProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.CompleteProblem;

public class CompleteProblemValidatorTests
{
    private CompleteProblemValidator _validator = null!;

    public CompleteProblemValidatorTests()
    {
        _validator = new CompleteProblemValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CompleteProblemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGameId_IsInvalid()
    {
        var command = new CompleteProblemCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyProblemId_IsInvalid()
    {
        var command = new CompleteProblemCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptySubmissionId_IsInvalid()
    {
        var command = new CompleteProblemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyRequestedByUserId_IsInvalid()
    {
        var command = new CompleteProblemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}