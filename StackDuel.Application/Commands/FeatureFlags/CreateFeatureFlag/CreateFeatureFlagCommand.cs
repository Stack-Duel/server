using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag;

internal sealed record CreateFeatureFlagCommand(string Key, string Name, string Description, bool DefaultEnabled)
    : ICommand<Guid>,
        IAuditableCommand
{
    public string AuditAction => "feature-flag.created";
    public string? AuditTargetType => "feature-flag";
    public string? AuditTargetId => Key;
    public object? AuditDetails =>
        new
        {
            Key,
            Name,
            Description,
            DefaultEnabled,
        };
}