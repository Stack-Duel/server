namespace StackDuel.Api.Requests.ValueTypes;

public sealed record UpdateValueTypeDefinitionRequest(string Name, string? Description, ValueTypeSpecRequest? Spec);