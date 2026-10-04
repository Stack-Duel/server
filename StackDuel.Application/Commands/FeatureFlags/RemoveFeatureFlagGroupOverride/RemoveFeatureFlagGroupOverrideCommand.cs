using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagGroupOverride;

internal sealed record RemoveFeatureFlagGroupOverrideCommand(Guid FlagId, Guid GroupId) : ICommand, IAuditableCommand
{
    public string AuditAction => "feature-flag.group-override-removed";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => FlagId.ToString();
    public object? AuditDetails => new { GroupId };
}