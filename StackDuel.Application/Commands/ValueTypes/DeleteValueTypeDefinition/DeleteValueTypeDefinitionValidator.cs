using FluentValidation;

namespace StackDuel.Application.Commands.ValueTypes.DeleteValueTypeDefinition;

internal sealed class DeleteValueTypeDefinitionValidator : AbstractValidator<DeleteValueTypeDefinitionCommand>
{
    public DeleteValueTypeDefinitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}