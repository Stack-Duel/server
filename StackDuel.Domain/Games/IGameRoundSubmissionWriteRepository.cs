using StackDuel.Domain.Games.Entities;

namespace StackDuel.Domain.Games;

public interface IGameRoundSubmissionWriteRepository
{
    Task AddAsync(GameRoundSubmission entity, CancellationToken cancellationToken = default);
}