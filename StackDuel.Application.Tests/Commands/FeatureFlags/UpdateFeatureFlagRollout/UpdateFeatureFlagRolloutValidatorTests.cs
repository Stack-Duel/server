using UpdateFeatureFlagRolloutCommand = StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout.UpdateFeatureFlagRolloutCommand;
using UpdateFeatureFlagRolloutValidator = StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout.UpdateFeatureFlagRolloutValidator;

namespace StackDuel.Application.Tests.Commands.FeatureFlags.UpdateFeatureFlagRollout;

public class UpdateFeatureFlagRolloutValidatorTests
{
    private UpdateFeatureFlagRolloutValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateFeatureFlagRolloutValidator();
    }

    [TestCase(0)]
    [TestCase(50)]
    [TestCase(100)]
    public void Validate_PercentageInRange_IsValid(int percentage)
    {
        var command = new UpdateFeatureFlagRolloutCommand(Guid.NewGuid(), percentage);
        Assert.That(_validator.Validate(command).IsValid, Is.True);
    }

    [TestCase(-1)]
    [TestCase(101)]
    public void Validate_PercentageOutOfRange_IsInvalid(int percentage)
    {
        var command = new UpdateFeatureFlagRolloutCommand(Guid.NewGuid(), percentage);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyFlagId_IsInvalid()
    {
        var command = new UpdateFeatureFlagRolloutCommand(Guid.Empty, 50);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }
}