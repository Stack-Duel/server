using StackDuel.Domain.ValueTypes.Enums;

namespace StackDuel.Domain.ValueTypes.ValueObjects;

public static class ValueTypeFields
{
    public const string Min = "min";
    public const string Max = "max";
    public const string LengthMin = "lengthMin";
    public const string LengthMax = "lengthMax";
    public const string Charset = "charset";
    public const string Element = "element";

    public static readonly ValueTypeFieldDescriptor MinField = new(Min, ValueTypeFieldValueKind.Number);
    public static readonly ValueTypeFieldDescriptor MaxField = new(Max, ValueTypeFieldValueKind.Number);
    public static readonly ValueTypeFieldDescriptor LengthMinField = new(LengthMin, ValueTypeFieldValueKind.Integer);
    public static readonly ValueTypeFieldDescriptor LengthMaxField = new(LengthMax, ValueTypeFieldValueKind.Integer);
    public static readonly ValueTypeFieldDescriptor CharsetField = new(Charset, ValueTypeFieldValueKind.Text);
    public static readonly ValueTypeFieldDescriptor ElementField = new(Element, ValueTypeFieldValueKind.ValueType);

    private static readonly IReadOnlyList<ValueTypeFieldDescriptor> NumericFields = [MinField, MaxField];
    private static readonly IReadOnlyList<ValueTypeFieldDescriptor> StringFields =
    [
        LengthMinField,
        LengthMaxField,
        CharsetField,
    ];
    private static readonly IReadOnlyList<ValueTypeFieldDescriptor> ArrayFields =
    [
        LengthMinField,
        LengthMaxField,
        ElementField,
    ];
    private static readonly IReadOnlyList<ValueTypeFieldDescriptor> NoFields = [];

    public static IReadOnlyList<ValueTypeFieldDescriptor> For(ValueTypeKind kind) =>
        kind switch
        {
            ValueTypeKind.Integer or ValueTypeKind.Double => NumericFields,
            ValueTypeKind.String => StringFields,
            ValueTypeKind.Array => ArrayFields,
            ValueTypeKind.Boolean => NoFields,
            _ => NoFields,
        };

    public static bool Accepts(ValueTypeKind kind, string field) =>
        For(kind).Any(descriptor => descriptor.Name == field);
}