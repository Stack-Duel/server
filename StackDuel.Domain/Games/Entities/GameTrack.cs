using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameTrack : Entity
{
    public GameTrack(Guid trackId, IEnumerable<Guid> languageIds)
    {
        if (trackId == Guid.Empty)
            throw new ArgumentException("Track id must not be empty.", nameof(trackId));

        TrackId = trackId;

        Guid[] languageIdArray =
            languageIds?.Where(id => id != Guid.Empty).Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(languageIds));

        foreach (Guid languageId in languageIdArray)
        {
            _languages.Add(new GameTrackLanguage(languageId));
        }
    }

    private GameTrack() { }

    public Guid TrackId { get; private set; }

    public IReadOnlyCollection<GameTrackLanguage> Languages => _languages.AsReadOnly();

    private readonly List<GameTrackLanguage> _languages = [];
}