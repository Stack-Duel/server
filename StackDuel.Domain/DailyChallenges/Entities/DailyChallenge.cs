using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.DailyChallenges.Entities;

public sealed class DailyChallenge : Entity
{
    public DailyChallenge(DateOnly challengeDate, Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        ChallengeDate = challengeDate;
        ProblemId = problemId;
    }

    private DailyChallenge() { }

    public void UpdateProblem(Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        ProblemId = problemId;
    }

    public DateOnly ChallengeDate { get; private set; }
    public Guid ProblemId { get; private set; }
}