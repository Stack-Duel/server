using StackDuel.Application.ValueTypes.Dtos;

namespace StackDuel.Application.Queries.ValueTypes.GetValueTypeDefinitions;

public sealed record GetValueTypeDefinitionsQuery : IQuery<IReadOnlyList<ValueTypeDefinitionDto>>;