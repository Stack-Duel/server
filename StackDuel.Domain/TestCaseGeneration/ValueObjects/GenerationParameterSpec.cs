using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.TestCaseGeneration.ValueObjects;

public sealed record GenerationParameterSpec(
    string Name,
    string ValueType,
    double? Min,
    double? Max,
    int? LengthMin,
    int? LengthMax,
    string? Charset,
    ValueTypeSpec? ResolvedType = null
)
{
    public string Name { get; } =
        !string.IsNullOrWhiteSpace(Name) ? Name : throw new ArgumentException("Name must not be empty.", nameof(Name));

    public string ValueType { get; } =
        !string.IsNullOrWhiteSpace(ValueType)
            ? ValueType
            : throw new ArgumentException("Value type must not be empty.", nameof(ValueType));
}