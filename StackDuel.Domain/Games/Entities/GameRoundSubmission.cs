using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

/// <summary>
/// One attempt: a single submission, by one participant, against one round, using one problem
/// setup. This is the relational replacement for what
/// <c>game_participants.problem_session</c> encoded as loose jsonb ids — the full attempt history
/// for a round is every row here matching (participant, round), in <see cref="CreatedAt"/> order,
/// rather than a single <c>activeSubmissionId</c> plus a solved-problem map.
/// </summary>
/// <remarks>
/// Its own aggregate rather than part of <see cref="Game"/>: it references a <c>Submission</c>,
/// which is a separate aggregate, so it is persisted independently rather than through the game's
/// write path.
/// </remarks>
public sealed class GameRoundSubmission : AggregateRoot
{
    /// <param name="problemId">
    /// Must be the problem that <paramref name="setupId"/> belongs to, which is also the problem of
    /// the round identified by <paramref name="roundId"/>.
    /// </param>
    /// <param name="gameId">Must be the game that both the round and the participant belong to.</param>
    /// <param name="userId">Must be the user that both the participant and the submission belong to.</param>
    /// <remarks>
    /// <paramref name="problemId"/>, <paramref name="gameId"/> and <paramref name="userId"/> are not
    /// independently meaningful. Each is shared by two composite foreign keys, so a row whose round
    /// and setup disagree about their problem, whose round and participant disagree about their
    /// game, or whose participant and submission disagree about their user, cannot be inserted at
    /// all — no value satisfies both constraints.
    /// </remarks>
    public GameRoundSubmission(
        Guid roundId,
        Guid participantId,
        Guid setupId,
        Guid submissionId,
        Guid problemId,
        Guid gameId,
        Guid userId
    )
    {
        if (roundId == Guid.Empty)
            throw new ArgumentException("Round id must not be empty.", nameof(roundId));

        if (participantId == Guid.Empty)
            throw new ArgumentException("Participant id must not be empty.", nameof(participantId));

        if (setupId == Guid.Empty)
            throw new ArgumentException("Setup id must not be empty.", nameof(setupId));

        if (submissionId == Guid.Empty)
            throw new ArgumentException("Submission id must not be empty.", nameof(submissionId));

        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (gameId == Guid.Empty)
            throw new ArgumentException("Game id must not be empty.", nameof(gameId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        RoundId = roundId;
        ParticipantId = participantId;
        SetupId = setupId;
        SubmissionId = submissionId;
        ProblemId = problemId;
        GameId = gameId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    private GameRoundSubmission() { }

    public Guid RoundId { get; private set; }

    public Guid ParticipantId { get; private set; }

    public Guid SetupId { get; private set; }

    public Guid SubmissionId { get; private set; }

    public Guid ProblemId { get; private set; }

    public Guid GameId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime CreatedAt { get; private set; }
}