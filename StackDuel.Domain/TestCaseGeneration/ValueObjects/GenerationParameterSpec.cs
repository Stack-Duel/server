namespace StackDuel.Domain.TestCaseGeneration.ValueObjects;

/// <summary>
/// Declarative constraint for one generated input parameter — data, not code, so it is
/// safe to accept from an untrusted problem author. Interpreted by a generator service
/// (e.g. one backed by Bogus) rather than executed.
/// </summary>
public sealed record GenerationParameterSpec(
    string Name,
    string ValueType,
    double? Min,
    double? Max,
    int? LengthMin,
    int? LengthMax,
    string? Charset
)
{
    public string Name { get; } =
        !string.IsNullOrWhiteSpace(Name) ? Name : throw new ArgumentException("Name must not be empty.", nameof(Name));

    public string ValueType { get; } =
        !string.IsNullOrWhiteSpace(ValueType)
            ? ValueType
            : throw new ArgumentException("Value type must not be empty.", nameof(ValueType));
}