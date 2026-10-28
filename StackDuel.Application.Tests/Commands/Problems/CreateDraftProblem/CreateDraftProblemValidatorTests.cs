using FluentValidation.Results;
using StackDuel.Application.Problems.Dtos;
using CreateDraftProblemCommand = StackDuel.Application.Commands.Problems.CreateDraftProblem.CreateDraftProblemCommand;
using CreateDraftProblemValidator = StackDuel.Application.Commands.Problems.CreateDraftProblem.CreateDraftProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.CreateDraftProblem;

public class CreateDraftProblemValidatorTests
{
    private static readonly string ValidQuestion = new('q', 60);
    private readonly CreateDraftProblemValidator _validator = new();

    private static CreateDraftProblemCommand ValidCommand(
        string title = "Two Sum",
        string question = "",
        int difficulty = 100,
        int timeLimitMs = 1000,
        int memoryLimitMb = 256,
        IEnumerable<string>? tags = null,
        Guid? trackId = null
    ) =>
        new(
            new CreateDraftProblemInput(
                title,
                question.Length > 0 ? question : ValidQuestion,
                tags ?? ["arrays"],
                difficulty,
                timeLimitMs,
                memoryLimitMb,
                trackId ?? Guid.NewGuid()
            )
        );

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyTitle_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(title: ""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_DifficultyBelowMinimum_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(difficulty: -1));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_TimeLimitOutOfRange_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(timeLimitMs: 1));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_MemoryLimitOutOfRange_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(memoryLimitMb: 1));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_NoTags_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(tags: []));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_TooManyTags_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(
            ValidCommand(tags: ["a", "b", "c", "d", "e", "f"])
        );

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_FiveTags_Passes()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(tags: ["a", "b", "c", "d", "e"]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TagWithInvalidFormat_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(tags: ["Invalid Tag"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyTrackId_Fails()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidCommand(trackId: Guid.Empty));

        Assert.False(result.IsValid);
    }
}