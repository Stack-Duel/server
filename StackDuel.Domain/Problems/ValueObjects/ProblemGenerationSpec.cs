using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record ProblemGenerationSpec(
    IReadOnlyList<GenerationParameterSpec> Parameters,
    string OutputValueType,
    int TargetCaseCount,
    int Seed
)
{
    public IReadOnlyList<GenerationParameterSpec> Parameters { get; } =
        Parameters is { Count: > 0 }
            ? Parameters
            : throw new ArgumentException("At least one parameter is required.", nameof(Parameters));

    public string OutputValueType { get; } =
        !string.IsNullOrWhiteSpace(OutputValueType)
            ? OutputValueType
            : throw new ArgumentException("Output value type must not be empty.", nameof(OutputValueType));

    public int TargetCaseCount { get; } =
        TargetCaseCount > 0
            ? TargetCaseCount
            : throw new ArgumentException("Target case count must be greater than zero.", nameof(TargetCaseCount));
}