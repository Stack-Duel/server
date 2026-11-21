namespace StackDuel.Domain.TestCaseGeneration.ValueObjects;

public static class GenerationValueTypes
{
    public const string Integer = "integer";
    public const string Double = "double";
    public const string Boolean = "boolean";
    public const string String = "string";
    public const string IntegerArray = "integer_array";

    public const int MaxLength = 1000;
    public const int MaxCharsetLength = 200;
    public const int MaxTargetCaseCount = 200;

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
    {
        Integer,
        Double,
        Boolean,
        String,
        IntegerArray,
    };

    public static bool IsSupported(string? valueType) =>
        !string.IsNullOrWhiteSpace(valueType) && Supported.Contains(valueType);

    public static bool SupportsRange(string valueType) =>
        valueType is Integer or Double or IntegerArray;

    public static bool SupportsLength(string valueType) => valueType is String or IntegerArray;

    public static bool SupportsCharset(string valueType) => valueType is String;
}