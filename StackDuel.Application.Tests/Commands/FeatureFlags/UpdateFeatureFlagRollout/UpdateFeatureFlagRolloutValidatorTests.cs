using UpdateFeatureFlagRolloutCommand = StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout.UpdateFeatureFlagRolloutCommand;
using UpdateFeatureFlagRolloutValidator = StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout.UpdateFeatureFlagRolloutValidator;

namespace StackDuel.Application.Tests.Commands.FeatureFlags.UpdateFeatureFlagRollout;

public class UpdateFeatureFlagRolloutValidatorTests
{
    private UpdateFeatureFlagRolloutValidator _validator = null!;

    public UpdateFeatureFlagRolloutValidatorTests()
    {
        _validator = new UpdateFeatureFlagRolloutValidator();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_PercentageInRange_IsValid(int percentage)
    {
        var command = new UpdateFeatureFlagRolloutCommand(Guid.NewGuid(), percentage);
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_PercentageOutOfRange_IsInvalid(int percentage)
    {
        var command = new UpdateFeatureFlagRolloutCommand(Guid.NewGuid(), percentage);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyFlagId_IsInvalid()
    {
        var command = new UpdateFeatureFlagRolloutCommand(Guid.Empty, 50);
        Assert.False(_validator.Validate(command).IsValid);
    }
}