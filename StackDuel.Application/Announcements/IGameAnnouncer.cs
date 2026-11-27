namespace StackDuel.Application.Announcements;

public interface IGameAnnouncer
{
    Task AnnounceGameCreatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}