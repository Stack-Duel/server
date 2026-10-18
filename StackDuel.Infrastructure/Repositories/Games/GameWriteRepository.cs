using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Games;

internal sealed class GameWriteRepository(StackDuelDbContext context) : IGameWriteRepository
{
    public async Task AddAsync(Game entity, CancellationToken cancellationToken = default)
    {
        await context.Games.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(Game game, CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsGameRoundPositionConflict(ex))
        {
            foreach (var entry in ex.Entries)
                entry.State = EntityState.Detached;

            throw new GameProblemPositionConflictException(game.Id, ex);
        }
    }

    private static bool IsGameRoundPositionConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: GameRound.GameIdPositionIndexName,
        };
}