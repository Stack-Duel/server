using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Domain.Tests.TestCaseGeneration.Entities;

public class TestCaseGenerationSpecTests
{
    private static readonly GenerationParameterSpec[] ValidParameters =
    [
        new("nums", "integer_array", Min: -10, Max: 10, LengthMin: 2, LengthMax: 6, Charset: null),
        new("target", "integer", Min: -10, Max: 10, LengthMin: null, LengthMax: null, Charset: null),
    ];

    private static TestCaseGenerationSpec CreateSpec(
        IReadOnlyList<GenerationParameterSpec>? parameters = null,
        int targetCaseCount = 30,
        int version = 1
    ) => new(Guid.NewGuid(), parameters ?? ValidParameters, "integer_array", targetCaseCount, seed: 123, version);

    [Fact]
    public void Constructor_ValidInputs_SetsProperties()
    {
        var spec = CreateSpec();

        Assert.Equal(2, spec.Parameters.Count);
        Assert.Equal("integer_array", spec.OutputValueType);
        Assert.Equal(30, spec.TargetCaseCount);
        Assert.Equal(123, spec.Seed);
        Assert.Equal(1, spec.Version);
    }

    [Fact]
    public void Constructor_NoParameters_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSpec(parameters: []));
    }

    [Fact]
    public void Constructor_NonPositiveTargetCaseCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSpec(targetCaseCount: 0));
    }

    [Fact]
    public void Constructor_NonPositiveVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSpec(version: 0));
    }
}