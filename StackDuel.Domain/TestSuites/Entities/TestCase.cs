using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Domain.TestSuites.Entities;

public sealed class TestCase : Entity
{
    internal TestCase(string name, string? description = null)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Name must not be empty.", nameof(name));

        Description = description;
        Source = TestCaseSource.Authored;
    }

    internal TestCase(string name, Guid generationSpecId, int generationCaseIndex, string? description = null)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Name must not be empty.", nameof(name));

        if (generationSpecId == Guid.Empty)
            throw new ArgumentException("Generation spec id must not be empty.", nameof(generationSpecId));

        if (generationCaseIndex < 0)
            throw new ArgumentException("Generation case index must not be negative.", nameof(generationCaseIndex));

        Description = description;
        Source = TestCaseSource.Generated;
        GenerationSpecId = generationSpecId;
        GenerationCaseIndex = generationCaseIndex;
    }

    public TestCaseInput AddInput(string value, string valueType)
    {
        var input = new TestCaseInput(value, valueType, _inputs.Count);
        _inputs.Add(input);
        return input;
    }

    public TestCaseExpectedOutput AddExpectedOutput(string value, string valueType)
    {
        var output = new TestCaseExpectedOutput(value, valueType);
        _expectedOutputs.Add(output);
        return output;
    }

    /// <summary>
    /// Marks this case retired instead of deleting it — a submission's result can reference
    /// a test case indefinitely (FK is ON DELETE RESTRICT), so a generated case that's ever
    /// been graded against can never actually be removed. Retired cases are excluded from
    /// future grading selection but keep existing for historical submissions to reference.
    /// </summary>
    public void Retire()
    {
        RetiredAt ??= DateTime.UtcNow;
    }

    private TestCase() { }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public TestCaseSource Source { get; private set; } = TestCaseSource.Authored;
    public Guid? GenerationSpecId { get; private set; }
    public int? GenerationCaseIndex { get; private set; }
    public DateTime? RetiredAt { get; private set; }

    public IReadOnlyCollection<TestCaseInput> Inputs => _inputs.AsReadOnly();
    public IReadOnlyCollection<TestCaseExpectedOutput> ExpectedOutputs => _expectedOutputs.AsReadOnly();

    private readonly List<TestCaseInput> _inputs = [];
    private readonly List<TestCaseExpectedOutput> _expectedOutputs = [];
}