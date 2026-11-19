using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ProblemValidation.Entities;

/// <summary>
/// One line of detail under a <see cref="ProblemValidationStep"/>, such as a single language's
/// reference solution result. Bounded by the problem's setup count.
/// </summary>
public sealed class ProblemValidationStepItem : Entity
{
    internal ProblemValidationStepItem(string label, ProblemValidationStepStatus status, string? detail)
    {
        Label = !string.IsNullOrWhiteSpace(label)
            ? label
            : throw new ArgumentException("Label must not be empty.", nameof(label));

        Status = status;
        Detail = detail;
    }

    private ProblemValidationStepItem() { }

    public string Label { get; private set; } = null!;
    public ProblemValidationStepStatus Status { get; private set; }
    public string? Detail { get; private set; }
}
