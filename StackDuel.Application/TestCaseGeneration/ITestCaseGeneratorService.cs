using StackDuel.Domain.TestCaseGeneration.Entities;

namespace StackDuel.Application.TestCaseGeneration;

public sealed record GeneratedValue(string Value, string ValueType);

/// <summary>
/// Deterministically produces input values for a generated test case from a
/// <see cref="TestCaseGenerationSpec"/>. Same spec + case index always yields the same
/// values, so a generated <see cref="StackDuel.Domain.TestSuites.Entities.TestCase"/> only
/// needs to persist which spec and index it came from — the input itself is regenerated
/// on demand instead of stored.
/// </summary>
public interface ITestCaseGeneratorService
{
    IReadOnlyList<GeneratedValue> GenerateInputs(TestCaseGenerationSpec spec, int caseIndex);
}