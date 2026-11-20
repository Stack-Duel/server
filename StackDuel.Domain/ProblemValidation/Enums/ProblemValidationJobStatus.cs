namespace StackDuel.Domain.ProblemValidation.Enums;

public enum ProblemValidationJobStatus
{
    Pending = 0,

    AwaitingGeneration = 1,

    Completed = 2,

    Failed = 3,

    Cancelled = 4,
}