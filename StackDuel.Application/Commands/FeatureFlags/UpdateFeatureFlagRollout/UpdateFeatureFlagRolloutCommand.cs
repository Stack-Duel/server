using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout;

internal sealed record UpdateFeatureFlagRolloutCommand(Guid FlagId, int RolloutPercentage) : ICommand, IAuditableCommand
{
    public string AuditAction => "feature-flag.rollout-updated";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => FlagId.ToString();
    public object? AuditDetails => new { RolloutPercentage };
}