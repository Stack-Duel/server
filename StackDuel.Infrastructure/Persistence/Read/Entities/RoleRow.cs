namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class RoleRow
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}