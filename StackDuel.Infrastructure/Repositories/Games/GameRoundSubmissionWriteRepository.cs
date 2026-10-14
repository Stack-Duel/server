using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Games;

internal sealed class GameRoundSubmissionWriteRepository(StackDuelDbContext context)
    : IGameRoundSubmissionWriteRepository
{
    public async Task AddAsync(GameRoundSubmission entity, CancellationToken cancellationToken = default)
    {
        await context.GameRoundSubmissions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}