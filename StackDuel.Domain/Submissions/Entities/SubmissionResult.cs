using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Domain.Submissions.Entities;

public sealed class SubmissionResult : Entity
{
    internal SubmissionResult(Guid testCaseId)
    {
        TestCaseId = testCaseId;
        Status = SubmissionResultStatus.Pending;
    }

    public void Update(
        SubmissionResultStatus status,
        int? runtime = null,
        int? memoryUsed = null,
        string? actualOutput = null,
        string? standardOutput = null,
        string? standardError = null,
        string? compileOutput = null
    )
    {
        Status = status;
        Runtime = runtime;
        MemoryUsed = memoryUsed;
        ActualOutput = actualOutput;
        StandardOutput = standardOutput;
        StandardError = standardError;
        CompileOutput = compileOutput;
    }

    public bool IsTerminal => Status is not SubmissionResultStatus.Pending and not SubmissionResultStatus.Processing;

    private SubmissionResult() { }

    public string? ActualOutput { get; private set; }
    public string? CompileOutput { get; private set; }
    public int? MemoryUsed { get; private set; }
    public int? Runtime { get; private set; }
    public string? StandardError { get; private set; }
    public string? StandardOutput { get; private set; }
    public SubmissionResultStatus Status { get; private set; }
    public Guid TestCaseId { get; private set; }
}