namespace StackDuel.Domain.Submissions.Enums;

public enum SubmissionResultStatus
{
    Pending,
    Processing,
    Accepted,
    WrongAnswer,
    TimeLimitExceeded,
    MemoryLimitExceeded,
    RuntimeError,
    CompileError,
}