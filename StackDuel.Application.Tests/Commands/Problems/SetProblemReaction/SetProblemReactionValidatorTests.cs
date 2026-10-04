using SetProblemReactionCommand = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionCommand;
using SetProblemReactionValidator = StackDuel.Application.Commands.Problems.SetProblemReaction.SetProblemReactionValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemReaction;

public class SetProblemReactionValidatorTests
{
    private SetProblemReactionValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SetProblemReactionValidator();
    }

    private static SetProblemReactionCommand ValidCommand() => new(Guid.NewGuid(), Guid.NewGuid(), "like");

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.That(_validator.Validate(ValidCommand()).IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyProblemId_IsInvalid()
    {
        var command = ValidCommand() with { ProblemId = Guid.Empty };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyReactionTypeKey_IsInvalid()
    {
        var command = ValidCommand() with { ReactionTypeKey = "" };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }
}