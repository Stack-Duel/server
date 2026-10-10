using FluentValidation.Results;
using StackDuel.Application.Games;
using CreateGameCommand = StackDuel.Application.Commands.Games.CreateGame.CreateGameCommand;
using CreateGameValidator = StackDuel.Application.Commands.Games.CreateGame.CreateGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.CreateGame;

public class CreateGameValidatorTests
{
    private CreateGameValidator _validator = null!;

    public CreateGameValidatorTests()
    {
        _validator = new CreateGameValidator();
    }

    private static TrackLanguageSelection[] Selections(params string[] trackKeys) =>
        [.. trackKeys.Select(key => new TrackLanguageSelection(key, [Guid.NewGuid()]))];

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateGameCommand("duel", Selections("general-purpose"), 600, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGameModeKey_IsInvalid()
    {
        var command = new CreateGameCommand(string.Empty, Selections("general-purpose"), 600, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTrackSelections_IsInvalid()
    {
        var command = new CreateGameCommand("duel", [], 600, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TrackSelectionWithEmptyTrackKey_IsInvalid()
    {
        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(string.Empty, [Guid.NewGuid()])],
            600,
            Guid.NewGuid()
        );

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TrackSelectionWithEmptyLanguageIds_IsInvalid()
    {
        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection("general-purpose", [])],
            600,
            Guid.NewGuid()
        );

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NonPositiveTimeLimit_IsInvalid()
    {
        var command = new CreateGameCommand("duel", Selections("general-purpose"), 0, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCreatedByUserId_IsInvalid()
    {
        var command = new CreateGameCommand("duel", Selections("general-purpose"), 600, Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}