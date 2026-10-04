using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagUserOverride;

internal sealed record RemoveFeatureFlagUserOverrideCommand(Guid FlagId, Guid UserId) : ICommand, IAuditableCommand
{
    public string AuditAction => "feature-flag.user-override-removed";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => FlagId.ToString();
    public object? AuditDetails => new { UserId };
}