using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition;
using StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition;
using StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition;
using StackDuel.Application.Queries.ValueTypes.GetValueTypeDefinitions;
using StackDuel.Application.Queries.ValueTypes.GetValueTypeMetadata;
using StackDuel.Application.ValueTypes.Dtos;

namespace StackDuel.Application.Services.ValueTypes;

public interface IValueTypeService
{
    Task<Result<IReadOnlyList<ValueTypeDefinitionDto>>> GetDefinitionsAsync(CancellationToken cancellationToken);

    Task<Result<ValueTypeMetadataDto>> GetMetadataAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateDefinitionAsync(
        string key,
        string name,
        string? description,
        ValueTypeSpecDto spec,
        CancellationToken cancellationToken
    );

    Task<Result> UpdateDefinitionAsync(
        Guid id,
        string name,
        string? description,
        ValueTypeSpecDto? spec,
        CancellationToken cancellationToken
    );

    Task<Result> DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken);
}

internal sealed class ValueTypeService(IMediator mediator) : IValueTypeService
{
    public async Task<Result<IReadOnlyList<ValueTypeDefinitionDto>>> GetDefinitionsAsync(
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetValueTypeDefinitionsQuery(), cancellationToken);

        return result;
    }

    public async Task<Result<ValueTypeMetadataDto>> GetMetadataAsync(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetValueTypeMetadataQuery(), cancellationToken);

        return result;
    }

    public async Task<Result<Guid>> CreateDefinitionAsync(
        string key,
        string name,
        string? description,
        ValueTypeSpecDto spec,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new CreateValueTypeDefinitionCommand(key, name, description, spec),
            cancellationToken
        );

        return result;
    }

    public async Task<Result> UpdateDefinitionAsync(
        Guid id,
        string name,
        string? description,
        ValueTypeSpecDto? spec,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new UpdateValueTypeDefinitionCommand(id, name, description, spec),
            cancellationToken
        );

        return result;
    }

    public async Task<Result> DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteValueTypeDefinitionCommand(id), cancellationToken);

        return result;
    }
}