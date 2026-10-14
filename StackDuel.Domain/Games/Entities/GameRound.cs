using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameRound : Entity
{
    public GameRound(int position, Guid problemId)
    {
        if (position < 0)
            throw new ArgumentException("Position must not be negative.", nameof(position));

        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        Position = position;
        ProblemId = problemId;
    }

    private GameRound() { }

    public int Position { get; private set; }

    public Guid ProblemId { get; private set; }
}