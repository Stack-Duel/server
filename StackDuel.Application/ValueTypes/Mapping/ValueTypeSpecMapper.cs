using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.Exceptions;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Application.ValueTypes.Mapping;

internal static class ValueTypeSpecMapper
{
    public static ValueTypeSpecDto ToDto(ValueTypeSpec spec) =>
        new(
            spec.Kind.ToString().ToLowerInvariant(),
            spec.Min,
            spec.Max,
            spec.LengthMin,
            spec.LengthMax,
            spec.Charset,
            spec.Element is null ? null : ToDto(spec.Element)
        );

    public static ValueTypeSpec ToSpec(ValueTypeSpecDto dto) =>
        new(
            ParseKind(dto.Kind),
            dto.Min,
            dto.Max,
            dto.LengthMin,
            dto.LengthMax,
            dto.Charset,
            dto.Element is null ? null : ToSpec(dto.Element)
        );

    public static ValueTypeDefinitionDto ToDto(ValueTypeDefinition definition) =>
        new(
            definition.Id,
            definition.Key.Value,
            definition.Name,
            definition.Description,
            ToDto(definition.Spec),
            definition.IsBuiltIn,
            ToDto(GenerationParameterFields.AcceptedBy(definition.Spec)),
            definition.CreatedAt,
            definition.UpdatedAt
        );

    public static IReadOnlyList<ValueTypeFieldDto> ToDto(IReadOnlyList<ValueTypeFieldDescriptor> fields) =>
        [.. fields.Select(field => new ValueTypeFieldDto(field.Name, field.ValueKind.ToString().ToLowerInvariant()))];

    private static ValueTypeKind ParseKind(string kind) =>
        Enum.TryParse(kind, ignoreCase: true, out ValueTypeKind parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new InvalidValueTypeDefinitionException($"Unknown value type kind '{kind}'.");
}