using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.TestCaseGeneration.ValueObjects;

public static class GenerationParameterFields
{
    public static IReadOnlyList<ValueTypeFieldDescriptor> AcceptedBy(ValueTypeSpec structure)
    {
        List<ValueTypeFieldDescriptor> accepted = [];

        if (ContainsNumber(structure))
        {
            accepted.Add(ValueTypeFields.MinField);
            accepted.Add(ValueTypeFields.MaxField);
        }

        if (HasOwnLength(structure))
        {
            accepted.Add(ValueTypeFields.LengthMinField);
            accepted.Add(ValueTypeFields.LengthMaxField);
        }

        if (ContainsString(structure))
            accepted.Add(ValueTypeFields.CharsetField);

        return accepted;
    }

    public static IReadOnlyList<string> UnsupportedOverrides(
        GenerationParameterSpec parameter,
        ValueTypeSpec structure
    )
    {
        HashSet<string> accepted = [.. AcceptedBy(structure).Select(descriptor => descriptor.Name)];

        return
        [
            .. SuppliedOverrides(parameter).Where(field => !accepted.Contains(field)),
        ];
    }

    private static IEnumerable<string> SuppliedOverrides(GenerationParameterSpec parameter)
    {
        if (parameter.Min.HasValue)
            yield return ValueTypeFields.Min;

        if (parameter.Max.HasValue)
            yield return ValueTypeFields.Max;

        if (parameter.LengthMin.HasValue)
            yield return ValueTypeFields.LengthMin;

        if (parameter.LengthMax.HasValue)
            yield return ValueTypeFields.LengthMax;

        if (parameter.Charset is not null)
            yield return ValueTypeFields.Charset;
    }

    private static bool HasOwnLength(ValueTypeSpec structure) =>
        structure.Kind is ValueTypeKind.String or ValueTypeKind.Array;

    private static bool ContainsNumber(ValueTypeSpec structure) =>
        structure.Kind switch
        {
            ValueTypeKind.Integer or ValueTypeKind.Double => true,
            ValueTypeKind.Array => structure.Element is not null && ContainsNumber(structure.Element),
            _ => false,
        };

    private static bool ContainsString(ValueTypeSpec structure) =>
        structure.Kind switch
        {
            ValueTypeKind.String => true,
            ValueTypeKind.Array => structure.Element is not null && ContainsString(structure.Element),
            _ => false,
        };
}