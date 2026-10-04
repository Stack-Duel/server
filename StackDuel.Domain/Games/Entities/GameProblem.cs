using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

/// <summary>
/// One slot in a game's shared problem sequence — every participant who reaches this
/// <see cref="Position"/> is given this same <see cref="ProblemId"/>. The sequence is built
/// lazily (see <see cref="Game.AppendProblem"/>): a position only exists once some participant
/// has actually reached it, so a fast solo player doesn't force problems to be generated for
/// rounds no one else will ever play.
/// </summary>
public sealed class GameProblem : Entity
{
    public GameProblem(int position, Guid problemId)
    {
        if (position < 0)
            throw new ArgumentException("Position must not be negative.", nameof(position));

        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        Position = position;
        ProblemId = problemId;
    }

    private GameProblem() { }

    public int Position { get; private set; }

    public Guid ProblemId { get; private set; }
}