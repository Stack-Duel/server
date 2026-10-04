namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class UserLanguagePreferenceRow
{
    public Guid UserId { get; set; }

    public Guid LanguageId { get; set; }

    public int Position { get; set; }
}