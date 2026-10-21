namespace StackDuel.Application.ValueTypes.Dtos;

public sealed record ValueTypeMetadataDto(
    IReadOnlyList<ValueTypeKindMetadataDto> Kinds,
    IReadOnlyList<ValueTypeDefinitionDto> Definitions
);