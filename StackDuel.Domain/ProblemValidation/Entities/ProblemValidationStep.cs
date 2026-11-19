using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ProblemValidation.Entities;

/// <summary>
/// One stage of a <see cref="ProblemValidationJob"/>, recorded so an author can see where a run
/// got to rather than only its final verdict. Created up front in <see cref="ProblemValidationStepKind"/>
/// order and then advanced in place, so a job never accumulates more than one row per kind.
/// </summary>
public sealed class ProblemValidationStep : Entity
{
    internal ProblemValidationStep(ProblemValidationStepKind kind)
    {
        Kind = kind;
        Status = ProblemValidationStepStatus.Pending;
    }

    private ProblemValidationStep() { }

    private readonly List<ProblemValidationStepItem> _items = [];

    public ProblemValidationStepKind Kind { get; private set; }
    public ProblemValidationStepStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Detail { get; private set; }
    public IReadOnlyCollection<ProblemValidationStepItem> Items => _items.AsReadOnly();

    internal void Begin()
    {
        if (Status != ProblemValidationStepStatus.Pending)
            return;

        Status = ProblemValidationStepStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    internal void Succeed(string? detail, IReadOnlyList<ProblemValidationStepItemSpec>? items) =>
        Finish(ProblemValidationStepStatus.Succeeded, detail, items);

    internal void Fail(string? detail, IReadOnlyList<ProblemValidationStepItemSpec>? items) =>
        Finish(ProblemValidationStepStatus.Failed, detail, items);

    internal void MarkCancelled()
    {
        Status = ProblemValidationStepStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    internal void Skip()
    {
        if (Status != ProblemValidationStepStatus.Pending)
            return;

        Status = ProblemValidationStepStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
    }

    private void Finish(
        ProblemValidationStepStatus status,
        string? detail,
        IReadOnlyList<ProblemValidationStepItemSpec>? items
    )
    {
        StartedAt ??= DateTime.UtcNow;
        Status = status;
        CompletedAt = DateTime.UtcNow;
        Detail = Truncate(detail);

        if (items is null)
            return;

        // A pass can be retried after a lease expires, so the lines are replaced rather than appended.
        _items.Clear();
        _items.AddRange(
            items.Select(item => new ProblemValidationStepItem(item.Label, item.Status, Truncate(item.Detail)))
        );
    }

    private const int MaxDetailLength = 2000;

    private static string? Truncate(string? detail) =>
        detail is not null && detail.Length > MaxDetailLength ? detail[..MaxDetailLength] : detail;
}

/// <summary>What a caller hands in to record a step's lines, before they become entities.</summary>
public sealed record ProblemValidationStepItemSpec(
    string Label,
    ProblemValidationStepStatus Status,
    string? Detail = null
);
