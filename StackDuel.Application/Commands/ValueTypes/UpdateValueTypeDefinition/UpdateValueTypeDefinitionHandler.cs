using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.ValueTypes.Mapping;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Exceptions;

namespace StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition;

internal sealed class UpdateValueTypeDefinitionHandler(
    IValidator<UpdateValueTypeDefinitionCommand> validator,
    IValueTypeDefinitionRepository valueTypeDefinitionRepository
) : AbstractCommandHandler<UpdateValueTypeDefinitionCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateValueTypeDefinitionCommand request,
        CancellationToken cancellationToken
    )
    {
        ValueTypeDefinition? definition = await valueTypeDefinitionRepository.FindByIdAsync(
            request.Id,
            cancellationToken
        );

        if (definition is null)
            return Result.NotFound();

        try
        {
            definition.Describe(request.Name, request.Description);

            if (request.Spec is not null)
                definition.Restructure(ValueTypeSpecMapper.ToSpec(request.Spec));
        }
        catch (InvalidValueTypeDefinitionException ex)
        {
            return Result.Invalid(new ValidationError(ex.Message));
        }

        await valueTypeDefinitionRepository.UpdateAsync(definition, cancellationToken);

        return Result.Success();
    }
}