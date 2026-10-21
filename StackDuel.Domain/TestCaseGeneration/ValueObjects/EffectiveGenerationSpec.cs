using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.TestCaseGeneration.ValueObjects;

public static class EffectiveGenerationSpec
{
    public static ValueTypeSpec For(GenerationParameterSpec parameter)
    {
        ValueTypeSpec structure = parameter.ResolvedType ?? FromBuiltInCatalog(parameter.ValueType);

        return ApplyOverrides(structure, parameter, isOutermost: true);
    }

    private static ValueTypeSpec FromBuiltInCatalog(string valueType)
    {
        WellKnownValueTypes.BuiltInValueType? builtIn = WellKnownValueTypes.All.FirstOrDefault(candidate =>
            candidate.Key == valueType
        );

        return builtIn?.Spec
            ?? throw new NotSupportedException($"Unsupported generation value type '{valueType}'.");
    }

    private static ValueTypeSpec ApplyOverrides(
        ValueTypeSpec spec,
        GenerationParameterSpec parameter,
        bool isOutermost
    ) =>
        spec.Kind switch
        {
            ValueTypeKind.Integer or ValueTypeKind.Double => new ValueTypeSpec(
                spec.Kind,
                min: parameter.Min ?? spec.Min,
                max: parameter.Max ?? spec.Max
            ),

            ValueTypeKind.String => new ValueTypeSpec(
                spec.Kind,
                lengthMin: isOutermost ? parameter.LengthMin ?? spec.LengthMin : spec.LengthMin,
                lengthMax: isOutermost ? parameter.LengthMax ?? spec.LengthMax : spec.LengthMax,
                charset: parameter.Charset ?? spec.Charset
            ),

            ValueTypeKind.Array => new ValueTypeSpec(
                ValueTypeKind.Array,
                lengthMin: isOutermost ? parameter.LengthMin ?? spec.LengthMin : spec.LengthMin,
                lengthMax: isOutermost ? parameter.LengthMax ?? spec.LengthMax : spec.LengthMax,
                element: ApplyOverrides(ElementOf(spec), parameter, isOutermost: false)
            ),

            _ => spec,
        };

    private static ValueTypeSpec ElementOf(ValueTypeSpec spec) =>
        spec.Element ?? throw new NotSupportedException("An array value type requires an element type.");
}