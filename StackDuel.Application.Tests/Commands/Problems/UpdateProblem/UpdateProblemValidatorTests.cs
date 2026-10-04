using StackDuel.Domain.Problems.Enums;
using FluentValidation.Results;
using UpdateProblemCommand = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemCommand;
using UpdateProblemValidator = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.UpdateProblem;

public class UpdateProblemValidatorTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private UpdateProblemValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateProblemValidator();
    }

    private static UpdateProblemCommand ValidCommand() =>
        new(Guid.NewGuid(), "Two Sum", ValidQuestion, 100, 1000, 256, ["arrays"], ProblemStatus.Published);

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
    public void Validate_TitleTooShort_IsInvalid()
    {
        var command = ValidCommand() with { Title = "ab" };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_TitleTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Title = new string('a', 201) };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_QuestionTooShort_IsInvalid()
    {
        var command = ValidCommand() with { Question = "too short" };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_DifficultyNegative_IsInvalid()
    {
        var command = ValidCommand() with { Difficulty = -1 };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [TestCase(99)]
    [TestCase(10001)]
    public void Validate_TimeLimitOutOfRange_IsInvalid(int timeLimitMs)
    {
        var command = ValidCommand() with { TimeLimitMs = timeLimitMs };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [TestCase(15)]
    [TestCase(513)]
    public void Validate_MemoryLimitOutOfRange_IsInvalid(int memoryLimitMb)
    {
        var command = ValidCommand() with { MemoryLimitMb = memoryLimitMb };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_NullTags_IsInvalid()
    {
        var command = ValidCommand() with { Tags = null! };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyTagString_IsInvalid()
    {
        var command = ValidCommand() with { Tags = [""] };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_TagTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Tags = [new string('a', 51)] };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_NullStatus_IsValid()
    {
        var command = ValidCommand() with { Status = null };
        Assert.That(_validator.Validate(command).IsValid, Is.True);
    }
}