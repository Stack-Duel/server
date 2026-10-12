using FluentValidation.Results;
using StackDuel.Domain.Problems.Enums;
using UpdateProblemCommand = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemCommand;
using UpdateProblemValidator = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.UpdateProblem;

public class UpdateProblemValidatorTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private UpdateProblemValidator _validator = null!;

    public UpdateProblemValidatorTests()
    {
        _validator = new UpdateProblemValidator();
    }

    private static UpdateProblemCommand ValidCommand() =>
        new(Guid.NewGuid(), "Two Sum", ValidQuestion, 100, 1000, 256, ["arrays"], ProblemStatus.Published);

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
    public void Validate_TitleTooShort_IsInvalid()
    {
        var command = ValidCommand() with { Title = "ab" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_TitleTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Title = new string('a', 201) };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_QuestionTooShort_IsInvalid()
    {
        var command = ValidCommand() with { Question = "too short" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_DifficultyNegative_IsInvalid()
    {
        var command = ValidCommand() with { Difficulty = -1 };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(10001)]
    public void Validate_TimeLimitOutOfRange_IsInvalid(int timeLimitMs)
    {
        var command = ValidCommand() with { TimeLimitMs = timeLimitMs };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(513)]
    public void Validate_MemoryLimitOutOfRange_IsInvalid(int memoryLimitMb)
    {
        var command = ValidCommand() with { MemoryLimitMb = memoryLimitMb };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullTags_IsInvalid()
    {
        var command = ValidCommand() with { Tags = null! };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyTagString_IsInvalid()
    {
        var command = ValidCommand() with { Tags = [""] };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_TagTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Tags = [new string('a', 51)] };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullStatus_IsValid()
    {
        var command = ValidCommand() with { Status = null };
        Assert.True(_validator.Validate(command).IsValid);
    }
}