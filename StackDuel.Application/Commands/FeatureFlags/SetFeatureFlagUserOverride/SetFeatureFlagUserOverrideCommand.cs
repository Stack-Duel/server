using StackDuel.Application.Audit;
using StackDuel.Application.Commands;
using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagUserOverride;

internal sealed record SetFeatureFlagUserOverrideCommand(Guid FlagId, Guid UserId, DecisionEffect Effect)
    : ICommand,
        IAuditableCommand
{
    public string AuditAction => "feature-flag.user-override-set";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => FlagId.ToString();
    public object? AuditDetails => new { UserId, Effect };
}