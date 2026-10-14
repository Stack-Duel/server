using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

/// <summary>
/// One round in a game's shared sequence — every participant who reaches this
/// <see cref="Position"/> is given this same <see cref="ProblemId"/>. The sequence is built lazily
/// (see <see cref="Game.AppendRound"/>): a position only exists once some participant has actually
/// reached it, so a fast solo player doesn't force problems to be generated for rounds no one else
/// will ever play.
/// </summary>
/// <remarks>
/// A round is unique both on (game, position) and on (game, problem): the latter is what lets
/// <c>game_round_submissions</c> pin a submission's setup to this round's problem through a
/// composite foreign key. <see cref="Game.AppendRound"/> enforces it in memory so a repeated
/// problem fails there rather than as a database error.
/// </remarks>
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