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

    [Test]
    public void Constructor_ValidInputs_SetsProperties()
    {
        var spec = CreateSpec();

        Assert.Multiple(() =>
        {
            Assert.That(spec.Parameters, Has.Count.EqualTo(2));
            Assert.That(spec.OutputValueType, Is.EqualTo("integer_array"));
            Assert.That(spec.TargetCaseCount, Is.EqualTo(30));
            Assert.That(spec.Seed, Is.EqualTo(123));
            Assert.That(spec.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void Constructor_NoParameters_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSpec(parameters: []));
    }

    [Test]
    public void Constructor_NonPositiveTargetCaseCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSpec(targetCaseCount: 0));
    }

    [Test]
    public void Constructor_NonPositiveVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSpec(version: 0));
    }
}