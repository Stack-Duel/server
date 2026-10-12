namespace StackDuel.Domain.Games.ValueObjects;

public sealed class GameProblemSession
{
    public GameProblemSession(Guid currentProblemId)
    {
        if (currentProblemId == Guid.Empty)
            throw new ArgumentException("Current problem id must not be empty.", nameof(currentProblemId));

        CurrentProblemId = currentProblemId;
        _solvedProblemIds = [];
        _skippedProblemIds = [];
    }

    public Guid CurrentProblemId { get; private set; }

    public IReadOnlyList<Guid> SolvedProblemIds => _solvedProblemIds.AsReadOnly();
    public IReadOnlyList<Guid> SkippedProblemIds => _skippedProblemIds.AsReadOnly();

    /// <summary>
    /// Every problem this participant should never be offered again: solved, skipped, or the one
    /// they are currently on.
    /// </summary>
    public IReadOnlyList<Guid> ExcludedProblemIds
    {
        get
        {
            List<Guid> excluded = [.. _solvedProblemIds, .. _skippedProblemIds];
            if (!excluded.Contains(CurrentProblemId))
                excluded.Add(CurrentProblemId);
            return excluded;
        }
    }

    private readonly List<Guid> _solvedProblemIds;
    private readonly List<Guid> _skippedProblemIds;

    public void AddSolvedProblem(Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (!_solvedProblemIds.Contains(problemId))
            _solvedProblemIds.Add(problemId);
    }

    public void AddSkippedProblem(Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (!_skippedProblemIds.Contains(problemId))
            _skippedProblemIds.Add(problemId);
    }

    public void AdvanceProblem(Guid newProblemId)
    {
        if (newProblemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(newProblemId));

        AddSolvedProblem(CurrentProblemId);
        CurrentProblemId = newProblemId;
    }
}