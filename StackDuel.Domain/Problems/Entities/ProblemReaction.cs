using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemReaction : AggregateRoot
{
    public ProblemReaction(Guid problemId, Guid userId, Guid reactionTypeId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (reactionTypeId == Guid.Empty)
            throw new ArgumentException("Reaction type id must not be empty.", nameof(reactionTypeId));

        ProblemId = problemId;
        UserId = userId;
        ReactionTypeId = reactionTypeId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    private ProblemReaction() { }

    /// <summary>
    /// Flips this permanent row back on. Reacting again after unreacting reuses the same row
    /// rather than inserting a new one, so a future notification hook here fires once per
    /// genuine "turned on" transition, not once per click.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid ProblemId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ReactionTypeId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}