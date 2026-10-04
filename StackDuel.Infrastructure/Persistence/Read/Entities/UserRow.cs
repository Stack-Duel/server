namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class UserRow
{
    public Guid Id { get; set; }

    public required string Sub { get; set; }

    public required string Username { get; set; }

    public string? Bio { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? UsernameLastChangedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SetupCompletedAt { get; set; }

    public bool IsPrivate { get; set; }
}