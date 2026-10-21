using Ardalis.Result;
using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Application.ValueTypes.Mapping;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Application.Queries.ValueTypes.GetValueTypeMetadata;

internal sealed class GetValueTypeMetadataHandler(IValueTypeDefinitionRepository valueTypeDefinitionRepository)
    : IQueryHandler<GetValueTypeMetadataQuery, ValueTypeMetadataDto>
{
    public async Task<Result<ValueTypeMetadataDto>> Handle(
        GetValueTypeMetadataQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ValueTypeDefinition> definitions = await valueTypeDefinitionRepository.GetAllOrderedAsync(
            cancellationToken
        );

        List<ValueTypeKindMetadataDto> kinds =
        [
            .. Enum
                .GetValues<ValueTypeKind>()
                .Select(kind => new ValueTypeKindMetadataDto(
                    kind.ToString().ToLowerInvariant(),
                    ValueTypeSpecMapper.ToDto(ValueTypeFields.For(kind))
                )),
        ];

        return Result.Success(
            new ValueTypeMetadataDto(kinds, [.. definitions.Select(ValueTypeSpecMapper.ToDto)])
        );
    }
}