using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Users.UpdateUserGroups;

internal sealed record UpdateUserGroupsCommand(Guid UserId, IReadOnlyCollection<Guid> GroupIds)
    : ICommand,
        IAuditableCommand
{
    public string AuditAction => "user.groups.updated";
    public string? AuditTargetType => "user";
    public string? AuditTargetId => UserId.ToString();
    public object? AuditDetails => new { GroupIds };
}