using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.ValueTypes;

public static class WellKnownValueTypes
{
    public const string Integer = "integer";
    public const string Double = "double";
    public const string Boolean = "boolean";
    public const string String = "string";
    public const string IntegerArray = "integer_array";

    public sealed record BuiltInValueType(string Key, string Name, string Description, ValueTypeSpec Spec);

    public static IReadOnlyList<BuiltInValueType> All =>
        [
            new(Integer, "Integer", "A whole number.", new ValueTypeSpec(ValueTypeKind.Integer)),
            new(Double, "Double", "A floating point number.", new ValueTypeSpec(ValueTypeKind.Double)),
            new(Boolean, "Boolean", "True or false.", new ValueTypeSpec(ValueTypeKind.Boolean)),
            new(String, "String", "A sequence of characters.", new ValueTypeSpec(ValueTypeKind.String)),
            new(
                IntegerArray,
                "Integer array",
                "A list of whole numbers.",
                new ValueTypeSpec(ValueTypeKind.Array, element: new ValueTypeSpec(ValueTypeKind.Integer))
            ),
        ];

    public static IReadOnlySet<string> Keys { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Integer, Double, Boolean, String, IntegerArray };
}