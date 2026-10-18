using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameRound : Entity
{
    public GameRound(Guid gameId, int position, Guid problemId)
    {
        if (gameId == Guid.Empty)
            throw new ArgumentException("Game id must not be empty.", nameof(gameId));

        if (position < 0)
            throw new ArgumentException("Position must not be negative.", nameof(position));

        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        GameId = gameId;
        Position = position;
        ProblemId = problemId;
    }

    private GameRound() { }

    public Guid GameId { get; private set; }

    public int Position { get; private set; }

    public Guid ProblemId { get; private set; }
}