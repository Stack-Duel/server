using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameRoundSolution : Entity
{
    public GameRoundSolution(Guid submissionId)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("Submission id must not be empty.", nameof(submissionId));

        SubmissionId = submissionId;
    }

    private GameRoundSolution() { }

    public Guid SubmissionId { get; private set; }
}