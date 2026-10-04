using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameTrackLanguage : Entity
{
    public GameTrackLanguage(Guid languageId)
    {
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language id must not be empty.", nameof(languageId));

        LanguageId = languageId;
    }

    private GameTrackLanguage() { }

    public Guid LanguageId { get; private set; }
}