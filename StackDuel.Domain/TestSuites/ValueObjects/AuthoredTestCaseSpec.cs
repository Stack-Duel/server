namespace StackDuel.Domain.TestSuites.ValueObjects;

/// <summary>One admin-authored sample test case, as submitted from the create-problem form.</summary>
public sealed record AuthoredTestCaseSpec(
    string? Name,
    IReadOnlyList<(string Value, string ValueType)> Inputs,
    string ExpectedOutputValue,
    string ExpectedOutputValueType
)
{
    public IReadOnlyList<(string Value, string ValueType)> Inputs { get; } =
        Inputs is { Count: > 0 }
            ? Inputs
            : throw new ArgumentException("At least one input is required.", nameof(Inputs));

    public string ExpectedOutputValue { get; } =
        !string.IsNullOrWhiteSpace(ExpectedOutputValue)
            ? ExpectedOutputValue
            : throw new ArgumentException("Expected output value must not be empty.", nameof(ExpectedOutputValue));

    public string ExpectedOutputValueType { get; } =
        !string.IsNullOrWhiteSpace(ExpectedOutputValueType)
            ? ExpectedOutputValueType
            : throw new ArgumentException(
                "Expected output value type must not be empty.",
                nameof(ExpectedOutputValueType)
            );
}