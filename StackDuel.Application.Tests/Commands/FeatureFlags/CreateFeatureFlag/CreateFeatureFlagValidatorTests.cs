using CreateFeatureFlagCommand = StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag.CreateFeatureFlagCommand;
using CreateFeatureFlagValidator = StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag.CreateFeatureFlagValidator;

namespace StackDuel.Application.Tests.Commands.FeatureFlags.CreateFeatureFlag;

public class CreateFeatureFlagValidatorTests
{
    private CreateFeatureFlagValidator _validator = null!;

    public CreateFeatureFlagValidatorTests()
    {
        _validator = new CreateFeatureFlagValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateFeatureFlagCommand("leaderboards", "Leaderboards", "Global kill-switch.", true);
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Leaderboards")]
    [InlineData("leader_boards")]
    [InlineData("leader boards")]
    public void Validate_InvalidKeyFormat_IsInvalid(string key)
    {
        var command = new CreateFeatureFlagCommand(key, "Name", "Description", false);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_BlankName_IsInvalid()
    {
        var command = new CreateFeatureFlagCommand("leaderboards", "  ", "Description", false);
        Assert.False(_validator.Validate(command).IsValid);
    }
}