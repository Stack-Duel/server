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
    public Guid? ActiveSubmissionId { get; private set; }

    public IReadOnlyList<Guid> SolvedProblemIds => _solvedProblemIds.AsReadOnly();
    public IReadOnlyList<Guid> SkippedProblemIds => _skippedProblemIds.AsReadOnly();
    public IReadOnlyDictionary<Guid, Guid> SolvedProblemSubmissionIds => _solvedProblemSubmissionIds;

    /// <summary>
    /// Every problem this participant should never be offered again: solved, skipped, or the one
    /// they're currently on. The single source of truth for "exclude" lists passed to
    /// IProblemSelectionStrategy — CompleteProblemHandler and SkipProblemHandler both read this
    /// instead of each re-deriving it, so a skipped problem can't quietly drop out of one path's
    /// exclusions while staying excluded in the other's.
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
    private readonly Dictionary<Guid, Guid> _solvedProblemSubmissionIds = new();

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

    public void RecordSolvedSubmission(Guid problemId, Guid submissionId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (submissionId == Guid.Empty)
            throw new ArgumentException("Submission id must not be empty.", nameof(submissionId));

        _solvedProblemSubmissionIds[problemId] = submissionId;
    }

    public void SetActiveSubmission(Guid submissionId)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("Submission id must not be empty.", nameof(submissionId));
        ActiveSubmissionId = submissionId;
    }

    /// <summary>
    /// Whether <paramref name="submissionId"/> is the one this session's current problem was
    /// actually submitted through — guards against completing a problem with a submission that
    /// wasn't created via the game's own submit endpoint.
    /// </summary>
    public bool HasActiveSubmission(Guid submissionId) => ActiveSubmissionId == submissionId;

    public void AdvanceProblem(Guid newProblemId)
    {
        if (newProblemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(newProblemId));

        // Add current problem to solved before advancing
        if (!_solvedProblemIds.Contains(CurrentProblemId))
            _solvedProblemIds.Add(CurrentProblemId);

        CurrentProblemId = newProblemId;
    }
}