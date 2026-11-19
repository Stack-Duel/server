namespace StackDuel.Domain.ProblemValidation.Enums;

public enum ProblemValidationStepStatus
{
    /// <summary>Not started — the run has not reached this stage.</summary>
    Pending = 0,

    Running = 1,
    Succeeded = 2,
    Failed = 3,

    /// <summary>Never reached because an earlier stage failed.</summary>
    Skipped = 4,

    /// <summary>Stopped part-way by the author.</summary>
    Cancelled = 5,
}
