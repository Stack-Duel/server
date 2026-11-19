namespace StackDuel.Domain.ProblemValidation.Enums;

/// <summary>
/// The ordered stages a <see cref="Entities.ProblemValidationJob"/> moves through. Fixed and
/// bounded: every job gets exactly one row per kind, so step storage grows with attempts, never
/// with problem size or generated case count.
/// </summary>
public enum ProblemValidationStepKind
{
    AuthorChecks = 0,
    ReferenceSolutions = 1,
    TestCaseGeneration = 2,
    Publish = 3,
}
