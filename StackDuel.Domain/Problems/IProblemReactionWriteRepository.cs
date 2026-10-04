using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Domain.Problems;

public interface IProblemReactionWriteRepository
{
    Task<ProblemReaction?> FindByProblemUserAndTypeAsync(
        Guid problemId,
        Guid userId,
        Guid reactionTypeId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the user's currently active reaction on this problem, if any. At most one can
    /// exist at a time — enforced by a partial unique index on (problem_id, user_id) where
    /// is_active — so this never has more than one row to return.
    /// </summary>
    Task<ProblemReaction?> FindActiveByProblemAndUserAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(ProblemReaction entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProblemReaction entity, CancellationToken cancellationToken = default);
}