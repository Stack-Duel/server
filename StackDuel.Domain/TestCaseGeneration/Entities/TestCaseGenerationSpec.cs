using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Domain.TestCaseGeneration.Entities;

/// <summary>
/// The recipe used to produce a problem's generated hidden test cases: the declarative
/// input constraints, the expected output's value type, how many cases to generate, and the
/// seed that makes generation reproducible. Never mutated after creation — editing a
/// problem's constraints creates a new spec (with a new <see cref="Seed"/> and incremented
/// <see cref="Version"/>) so already-generated <see cref="TestSuites.Entities.TestCase"/>s
/// stay reproducible against the exact spec they were generated from.
/// </summary>
public sealed class TestCaseGenerationSpec : AggregateRoot
{
    public TestCaseGenerationSpec(
        Guid problemSetupId,
        IReadOnlyList<GenerationParameterSpec> parameters,
        string outputValueType,
        int targetCaseCount,
        int seed,
        int version
    )
    {
        ProblemSetupId =
            problemSetupId != Guid.Empty
                ? problemSetupId
                : throw new ArgumentException("Problem setup id must not be empty.", nameof(problemSetupId));

        Parameters = parameters is { Count: > 0 }
            ? parameters
            : throw new ArgumentException("At least one parameter is required.", nameof(parameters));

        OutputValueType = !string.IsNullOrWhiteSpace(outputValueType)
            ? outputValueType
            : throw new ArgumentException("Output value type must not be empty.", nameof(outputValueType));

        TargetCaseCount =
            targetCaseCount > 0
                ? targetCaseCount
                : throw new ArgumentException("Target case count must be greater than zero.", nameof(targetCaseCount));

        Seed = seed;

        Version =
            version > 0 ? version : throw new ArgumentException("Version must be greater than zero.", nameof(version));

        CreatedAt = DateTime.UtcNow;
    }

    private TestCaseGenerationSpec() { }

    public Guid ProblemSetupId { get; private set; }
    public IReadOnlyList<GenerationParameterSpec> Parameters { get; private set; } = [];
    public string OutputValueType { get; private set; } = null!;
    public int TargetCaseCount { get; private set; }
    public int Seed { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
}