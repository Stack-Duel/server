using SetProblemReactionCommand = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionCommand;
using SetProblemReactionValidator = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemReaction;

public class SetProblemReactionValidatorTests
{
    private SetProblemReactionValidator _validator = null!;

    public SetProblemReactionValidatorTests()
    {
        _validator = new SetProblemReactionValidator();
    }

    private static SetProblemReactionCommand ValidCommand() => new(Guid.NewGuid(), Guid.NewGuid(), "like");

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyProblemId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemId = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyReactionTypeKey_IsInvalid()
    {
        var command = ValidCommand() with { ReactionTypeKey = "" };
        Assert.False(_validator.Validate(command).IsValid);
    }
}