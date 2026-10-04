using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemPoolItem : Entity
{
    public ProblemPoolItem(Guid problemId, int position)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        ProblemId = problemId;
        Position = position;
        AddedAt = DateTime.UtcNow;
    }

    private ProblemPoolItem() { }

    internal void SetPosition(int position)
    {
        Position = position;
    }

    public Guid ProblemId { get; private set; }

    public int Position { get; private set; }

    public DateTime AddedAt { get; private set; }
}