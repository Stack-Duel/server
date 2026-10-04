namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class PermissionRow
{
    public Guid Id { get; set; }

    public required string Code { get; set; }
}