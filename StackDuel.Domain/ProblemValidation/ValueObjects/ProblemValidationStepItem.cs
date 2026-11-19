using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Domain.ProblemValidation.ValueObjects;

/// <summary>
/// One line of detail under a step, such as a single language's reference solution result.
/// Stored as jsonb on the step rather than as its own table: the list is read whole for display,
/// is bounded by the problem's setup count, and is never queried across jobs.
/// </summary>
public sealed record ProblemValidationStepItem(
    string Label,
    ProblemValidationStepStatus Status,
    string? Detail = null
);
