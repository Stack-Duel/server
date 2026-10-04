using CreateFeatureFlagCommand = StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag.CreateFeatureFlagCommand;
using CreateFeatureFlagValidator = StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag.CreateFeatureFlagValidator;

namespace StackDuel.Application.Tests.Commands.FeatureFlags.CreateFeatureFlag;

public class CreateFeatureFlagValidatorTests
{
    private CreateFeatureFlagValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateFeatureFlagValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateFeatureFlagCommand("leaderboards", "Leaderboards", "Global kill-switch.", true);
        Assert.That(_validator.Validate(command).IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("Leaderboards")]
    [TestCase("leader_boards")]
    [TestCase("leader boards")]
    public void Validate_InvalidKeyFormat_IsInvalid(string key)
    {
        var command = new CreateFeatureFlagCommand(key, "Name", "Description", false);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_BlankName_IsInvalid()
    {
        var command = new CreateFeatureFlagCommand("leaderboards", "  ", "Description", false);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }
}