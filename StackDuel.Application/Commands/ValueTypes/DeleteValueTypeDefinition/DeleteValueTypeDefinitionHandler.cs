using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;

namespace StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition;

internal sealed class DeleteValueTypeDefinitionHandler(
    IValidator<DeleteValueTypeDefinitionCommand> validator,
    IValueTypeDefinitionRepository valueTypeDefinitionRepository,
    IValueTypeUsageRepository valueTypeUsageRepository
) : AbstractCommandHandler<DeleteValueTypeDefinitionCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        DeleteValueTypeDefinitionCommand request,
        CancellationToken cancellationToken
    )
    {
        ValueTypeDefinition? definition = await valueTypeDefinitionRepository.FindByIdAsync(
            request.Id,
            cancellationToken
        );

        if (definition is null)
            return Result.NotFound();

        if (definition.IsBuiltIn)
            return Result.Invalid(new ValidationError("Built-in value types cannot be deleted."));

        bool isInUse = await valueTypeUsageRepository.IsKeyInUseAsync(definition.Key.Value, cancellationToken);

        if (isInUse)
            return Result.Invalid(
                new ValidationError("This value type is still referenced by a problem and cannot be deleted.")
            );

        await valueTypeDefinitionRepository.RemoveAsync(definition, cancellationToken);

        return Result.Success();
    }
}