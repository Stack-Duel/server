namespace StackDuel.Api.Requests.ValueTypes;

public sealed record CreateValueTypeDefinitionRequest(
    string Key,
    string Name,
    string? Description,
    ValueTypeSpecRequest Spec
);