namespace StackDuel.Application.ValueTypes.Dtos;

public sealed record ValueTypeKindMetadataDto(string Kind, IReadOnlyList<ValueTypeFieldDto> Fields);