using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameRoundSubmission : AggregateRoot
{
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