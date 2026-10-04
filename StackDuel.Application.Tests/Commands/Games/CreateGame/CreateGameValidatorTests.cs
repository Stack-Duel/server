using StackDuel.Application.Games;
using FluentValidation.Results;
using CreateGameCommand = StackDuel.Application.Commands.Games.CreateGame.CreateGameCommand;
using CreateGameValidator = StackDuel.Application.Commands.Games.CreateGame.CreateGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.CreateGame;

public class CreateGameValidatorTests
{
    private CreateGameValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateGameValidator();
    }

    private static TrackLanguageSelection[] Selections(params string[] trackKeys) =>
        [.. trackKeys.Select(key => new TrackLanguageSelection(key, [Guid.NewGuid()]))];

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateGameCommand("duel", Selections("general-purpose"), 600, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyGameModeKey_IsInvalid()
    {
        var command = new CreateGameCommand(string.Empty, Selections("general-purpose"), 600, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyTrackSelections_IsInvalid()
    {
        var command = new CreateGameCommand("duel", [], 600, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_TrackSelectionWithEmptyTrackKey_IsInvalid()
    {
        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(string.Empty, [Guid.NewGuid()])],
            600,
            Guid.NewGuid()
        );

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_TrackSelectionWithEmptyLanguageIds_IsInvalid()
    {
        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection("general-purpose", [])],
            600,
            Guid.NewGuid()
        );

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_NonPositiveTimeLimit_IsInvalid()
    {
        var command = new CreateGameCommand("duel", Selections("general-purpose"), 0, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyCreatedByUserId_IsInvalid()
    {
        var command = new CreateGameCommand("duel", Selections("general-purpose"), 600, Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}