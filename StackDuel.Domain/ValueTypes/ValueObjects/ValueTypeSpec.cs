using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.Exceptions;
using System.Text.Json.Serialization;

namespace StackDuel.Domain.ValueTypes.ValueObjects;

public sealed record ValueTypeSpec
{
    public const int MaxDepth = 5;
    public const int MaxCharsetLength = 200;

    public ValueTypeSpec(
        ValueTypeKind kind,
        double? min = null,
        double? max = null,
        int? lengthMin = null,
        int? lengthMax = null,
        string? charset = null,
        ValueTypeSpec? element = null
    )
    {
        if (!Enum.IsDefined(kind))
            throw new InvalidValueTypeDefinitionException($"Unknown value type kind '{kind}'.");

        Kind = kind;
        Min = min;
        Max = max;
        LengthMin = lengthMin;
        LengthMax = lengthMax;
        Charset = charset;
        Element = element;

        RejectFieldsTheKindDoesNotAccept();
        RejectInvertedBounds();
        RejectBlankCharset();
        RejectExcessiveDepth();
    }

    public ValueTypeKind Kind { get; }
    public double? Min { get; }
    public double? Max { get; }
    public int? LengthMin { get; }
    public int? LengthMax { get; }
    public string? Charset { get; }
    public ValueTypeSpec? Element { get; }

    [JsonIgnore]
    public int Depth => Element is null ? 1 : Element.Depth + 1;

    private void RejectFieldsTheKindDoesNotAccept()
    {
        foreach ((string field, bool isSupplied) in SuppliedFields())
        {
            if (isSupplied && !ValueTypeFields.Accepts(Kind, field))
                throw new InvalidValueTypeDefinitionException(
                    $"'{field}' does not apply to a {Kind} value type."
                );
        }

        if (Kind == ValueTypeKind.Array && Element is null)
            throw new InvalidValueTypeDefinitionException("An array value type requires an element type.");
    }

    private IEnumerable<(string Field, bool IsSupplied)> SuppliedFields() =>
        [
            (ValueTypeFields.Min, Min.HasValue),
            (ValueTypeFields.Max, Max.HasValue),
            (ValueTypeFields.LengthMin, LengthMin.HasValue),
            (ValueTypeFields.LengthMax, LengthMax.HasValue),
            (ValueTypeFields.Charset, Charset is not null),
            (ValueTypeFields.Element, Element is not null),
        ];

    private void RejectInvertedBounds()
    {
        if (Min.HasValue && Max.HasValue && Min.Value > Max.Value)
            throw new InvalidValueTypeDefinitionException("Min must not be greater than max.");

        if (LengthMin.HasValue && LengthMax.HasValue && LengthMin.Value > LengthMax.Value)
            throw new InvalidValueTypeDefinitionException("Length min must not be greater than length max.");

        if (LengthMin is < 0 || LengthMax is < 0)
            throw new InvalidValueTypeDefinitionException("Lengths must not be negative.");
    }

    private void RejectBlankCharset()
    {
        if (Charset is null)
            return;

        if (Charset.Length == 0)
            throw new InvalidValueTypeDefinitionException("Charset must not be empty when provided.");

        if (Charset.Length > MaxCharsetLength)
            throw new InvalidValueTypeDefinitionException(
                $"Charset must be at most {MaxCharsetLength} characters."
            );
    }

    private void RejectExcessiveDepth()
    {
        if (Depth > MaxDepth)
            throw new InvalidValueTypeDefinitionException($"A value type may nest at most {MaxDepth} levels deep.");
    }
}