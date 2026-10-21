using StackDuel.Application.ValueTypes.Dtos;

namespace StackDuel.Application.Queries.ValueTypes.GetValueTypeMetadata;

public sealed record GetValueTypeMetadataQuery : IQuery<ValueTypeMetadataDto>;