namespace StackDuel.Domain.ProblemValidation.Enums;

public enum ProblemValidationJobStatus
{
    /// <summary>Not yet validated.</summary>
    Pending = 0,

    /// <summary>Reference solutions validated; waiting on the enqueued TestCaseGenerationJobs.</summary>
    AwaitingGeneration = 1,

    /// <summary>All setups generated successfully — the problem has been published.</summary>
    Completed = 2,

    Failed = 3,
}