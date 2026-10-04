using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.TestSuites.Entities;

public sealed class TestCaseInput : Entity
{
    internal TestCaseInput(string value, string valueType, int position)
    {
        Value = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Value must not be empty.", nameof(value));

        ValueType = !string.IsNullOrWhiteSpace(valueType)
            ? valueType
            : throw new ArgumentException("Value type must not be empty.", nameof(valueType));

        Position =
            position >= 0 ? position : throw new ArgumentException("Position must not be negative.", nameof(position));
    }

    private TestCaseInput() { }

    public string Value { get; private set; } = null!;
    public string ValueType { get; private set; } = null!;

    /// <summary>
    /// This input's zero-based position among the test case's other inputs — the order the
    /// function's arguments must be passed in. Inputs are read back from the database
    /// unordered by default, so every read site must order by this explicitly.
    /// </summary>
    public int Position { get; private set; }
}