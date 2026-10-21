using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.ValueTypes.Mapping;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Exceptions;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition;

internal sealed class CreateValueTypeDefinitionHandler(
    IValidator<CreateValueTypeDefinitionCommand> validator,
    IValueTypeDefinitionRepository valueTypeDefinitionRepository
) : AbstractCommandHandler<CreateValueTypeDefinitionCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateValueTypeDefinitionCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ValueTypeKey key = new(request.Key);

            ValueTypeDefinition? existing = await valueTypeDefinitionRepository.FindByKeyAsync(
                key,
                cancellationToken
            );

            if (existing is not null)
                return Result.Invalid(
                    new ValidationError("Key", $"A value type with key '{key.Value}' already exists.")
                );

            ValueTypeDefinition definition = new(
                key,
                request.Name,
                request.Description,
                ValueTypeSpecMapper.ToSpec(request.Spec),
                isBuiltIn: false
            );

            await valueTypeDefinitionRepository.AddAsync(definition, cancellationToken);

            return Result.Success(definition.Id);
        }
        catch (InvalidValueTypeDefinitionException ex)
        {
            return Result.Invalid(new ValidationError(ex.Message));
        }
    }
}