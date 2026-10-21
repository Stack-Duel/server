namespace StackDuel.Application.ValueTypes.Dtos;

public sealed record ValueTypeDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    ValueTypeSpecDto Spec,
    bool IsBuiltIn,
    IReadOnlyList<ValueTypeFieldDto> ParameterFields,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);