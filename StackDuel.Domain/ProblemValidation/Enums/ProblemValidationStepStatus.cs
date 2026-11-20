namespace StackDuel.Domain.ProblemValidation.Enums;

public enum ProblemValidationStepStatus
{
    Pending = 0,

    Running = 1,
    Succeeded = 2,
    Failed = 3,

    Skipped = 4,

    Cancelled = 5,
}
