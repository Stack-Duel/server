using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagDefault;

internal sealed record UpdateFeatureFlagDefaultCommand(Guid FlagId, bool DefaultEnabled) : ICommand, IAuditableCommand
{
    public string AuditAction => "feature-flag.default-updated";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => FlagId.ToString();
    public object? AuditDetails => new { DefaultEnabled };
}