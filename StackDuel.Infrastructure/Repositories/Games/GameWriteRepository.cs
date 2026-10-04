using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Games;

internal sealed class GameWriteRepository(StackDuelDbContext context) : IGameWriteRepository
{
    private const string ProblemSequencePositionIndexName = "IX_game_problems_game_id_position";

    public async Task AddAsync(Game entity, CancellationToken cancellationToken = default)
    {
        await context.Games.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Game?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Games.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(Game entity, CancellationToken cancellationToken = default)
    {
        // Every Entity subclass gets its Id assigned client-side (Entity's constructor calls
        // Guid.NewGuid()), so whenever a new child is appended to an already-tracked collection
        // (Game.Join() -> Participants, Game.AppendProblem() -> ProblemSequence), EF's change
        // detection can't tell it's new from a default/temporary key value — it sees a real Guid
        // already set and assumes the row already exists, generating an UPDATE instead of an
        // INSERT. That UPDATE matches 0 rows and throws DbUpdateConcurrencyException.
        //
        // Auto-detect-changes is disabled for this loop because context.Entry(...) itself triggers
        // a full change-detection pass — with it left on, the very first Entry() call (for a
        // still-Unchanged existing row) would already discover and misclassify the new ones as
        // Modified before this loop ever reaches them, making the Detached check below always
        // false. With it off, Entry() reports each entity's real tracked-or-not status, so only
        // genuinely new rows get forced to Added; re-enabling it before SaveChanges still detects
        // ordinary scalar edits (e.g. score, problem session) on existing rows.
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        foreach (EntityEntry<GameParticipant> entry in entity.Participants.Select(context.Entry))
        {
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }
        foreach (EntityEntry<GameProblem> entry in entity.ProblemSequence.Select(context.Entry))
        {
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }
        context.ChangeTracker.AutoDetectChangesEnabled = true;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsProblemSequencePositionConflict(ex))
        {
            // The losing side of the (game_id, position) unique index race (see
            // GameProblemConfiguration). The failed SaveChanges leaves every entity from this
            // attempt sitting in the context's change tracker in whatever state it was pushed to
            // (Added/Modified) — detach the whole graph so a retry's fresh FindGameByIdAsync, on
            // this same scoped DbContext, actually hits the database instead of being handed back
            // this same stale, half-failed instance by EF's identity map.
            DetachGraph(entity);
            throw new GameProblemPositionConflictException(entity.Id, ex);
        }
    }

    private static bool IsProblemSequencePositionConflict(DbUpdateException ex) =>
        ex.InnerException
            is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ProblemSequencePositionIndexName
        };

    private void DetachGraph(Game entity)
    {
        context.Entry(entity).State = EntityState.Detached;
        foreach (GameParticipant participant in entity.Participants)
            context.Entry(participant).State = EntityState.Detached;
        foreach (GameProblem problem in entity.ProblemSequence)
            context.Entry(problem).State = EntityState.Detached;
        foreach (GameTrack track in entity.Tracks)
            context.Entry(track).State = EntityState.Detached;
    }
}