using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;
using StackDuel.Domain.Submissions.Exceptions;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Submissions.Entities;

public sealed class Submission : AggregateRoot
{
    public Submission(
        Guid userId,
        Guid problemSetupId,
        SubmissionType type,
        SourceCode sourceCode,
        IEnumerable<Guid> testCaseIds,
        IEnumerable<SubmissionSourceFile>? additionalFiles = null,
        Guid? gameId = null
    )
    {
        UserId = userId;
        ProblemSetupId = problemSetupId;
        GameId = gameId;
        Type = type;
        SourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
        Status = SubmissionStatus.Queued;
        CreatedAt = DateTime.UtcNow;
        AdditionalFiles = additionalFiles is null ? [] : [.. additionalFiles];

        foreach (Guid testCaseId in testCaseIds)
            _results.Add(new SubmissionResult(testCaseId));

        AddDomainEvent(new SubmissionCreatedDomainEvent(Id));
    }

    public void Complete()
    {
        if (_results.Any(r => !r.IsTerminal))
            throw new SubmissionNotCompleteException();

        Status = _results.All(r => r.Status == SubmissionResultStatus.Accepted)
            ? SubmissionStatus.Accepted
            : SubmissionStatus.WrongAnswer;

        AddDomainEvent(new SubmissionCompletedDomainEvent(UserId, Id, Status, GameId));
    }

    /// <summary>
    /// Marks all pending/processing results as <see cref="SubmissionResultStatus.RuntimeError"/>
    /// and transitions the submission to <see cref="SubmissionStatus.WrongAnswer"/>.
    /// Called when the pipeline job fails before <see cref="EvaluateStepHandler"/> can run.
    /// </summary>
    public void Fail()
    {
        foreach (var result in _results.Where(r => !r.IsTerminal))
            result.Update(SubmissionResultStatus.RuntimeError);

        Status = SubmissionStatus.WrongAnswer;

        AddDomainEvent(new SubmissionCompletedDomainEvent(UserId, Id, Status, GameId));
    }

    public void StartRunning()
    {
        if (Status != SubmissionStatus.Queued)
            throw new InvalidSubmissionStateException("Only a queued submission can be started.");

        Status = SubmissionStatus.Running;
    }

    public void UpdateResult(
        Guid testCaseId,
        SubmissionResultStatus status,
        int? runtime = null,
        int? memoryUsed = null,
        string? actualOutput = null,
        string? standardOutput = null,
        string? standardError = null,
        string? compileOutput = null
    )
    {
        SubmissionResult result =
            _results.FirstOrDefault(r => r.TestCaseId == testCaseId)
            ?? throw new SubmissionResultNotFoundException(testCaseId);

        result.Update(status, runtime, memoryUsed, actualOutput, standardOutput, standardError, compileOutput);
    }

    private Submission() { }

    public DateTime CreatedAt { get; private set; }

    public int? ExecutionTime => Results.Max(r => r.Runtime);

    public int? MemoryUsage => Results.Max(r => r.MemoryUsed);

    public Guid ProblemSetupId { get; private set; }
    public Guid? GameId { get; private set; }
    public IReadOnlyCollection<SubmissionResult> Results => _results.AsReadOnly();
    public SourceCode SourceCode { get; private set; } = null!;
    public SubmissionStatus Status { get; private set; }
    public SubmissionType Type { get; private set; }
    public Guid UserId { get; private set; }
    public IReadOnlyList<SubmissionSourceFile> AdditionalFiles { get; private set; } = [];

    private readonly List<SubmissionResult> _results = [];
}