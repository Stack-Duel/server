using Ardalis.Result;
using StackDuel.Application.ValueTypes.Dtos;
using StackDuel.Application.ValueTypes.Mapping;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;

namespace StackDuel.Application.Queries.ValueTypes.GetValueTypeDefinitions;

internal sealed class GetValueTypeDefinitionsHandler(IValueTypeDefinitionRepository valueTypeDefinitionRepository)
    : IQueryHandler<GetValueTypeDefinitionsQuery, IReadOnlyList<ValueTypeDefinitionDto>>
{
    public async Task<Result<IReadOnlyList<ValueTypeDefinitionDto>>> Handle(
        GetValueTypeDefinitionsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ValueTypeDefinition> definitions = await valueTypeDefinitionRepository.GetAllOrderedAsync(
            cancellationToken
        );

        return Result.Success<IReadOnlyList<ValueTypeDefinitionDto>>(
            [.. definitions.Select(ValueTypeSpecMapper.ToDto)]
        );
    }
}