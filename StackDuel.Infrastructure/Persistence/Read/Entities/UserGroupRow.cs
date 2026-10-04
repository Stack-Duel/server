namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class UserGroupRow
{
    public Guid UserId { get; set; }

    public Guid GroupId { get; set; }
}