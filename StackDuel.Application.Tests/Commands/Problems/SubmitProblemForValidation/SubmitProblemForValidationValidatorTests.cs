using FluentValidation.Results;
using SubmitProblemForValidationCommand = StackDuel.Application.Commands.Problems.SubmitProblemForValidation.SubmitProblemForValidationCommand;
using SubmitProblemForValidationValidator = StackDuel.Application.Commands.Problems.SubmitProblemForValidation.SubmitProblemForValidationValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SubmitProblemForValidation;

public class SubmitProblemForValidationValidatorTests
{
    private readonly SubmitProblemForValidationValidator _validator = new();

    [Fact]
    public async Task Validate_ProblemIdPresent_Passes()
    {
        ValidationResult result = await _validator.ValidateAsync(new SubmitProblemForValidationCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyProblemId_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(new SubmitProblemForValidationCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(SubmitProblemForValidationCommand.ProblemId), Assert.Single(result.Errors).PropertyName);
    }
}