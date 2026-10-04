using StackDuel.Application.Audit;
using StackDuel.Application.Commands;
using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagGroupOverride;

internal sealed record SetFeatureFlagGroupOverrideCommand(Guid FlagId, Guid GroupId, DecisionEffect Effect)
    : ICommand,
        IAuditableCommand
{
    public string AuditAction => "feature-flag.group-override-set";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => FlagId.ToString();
    public object? AuditDetails => new { GroupId, Effect };
}