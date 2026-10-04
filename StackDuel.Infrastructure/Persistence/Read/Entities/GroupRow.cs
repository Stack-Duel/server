namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class GroupRow
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}