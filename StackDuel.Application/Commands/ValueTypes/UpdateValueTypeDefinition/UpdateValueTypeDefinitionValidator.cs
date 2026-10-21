using FluentValidation;
using StackDuel.Domain.ValueTypes.Entities;

namespace StackDuel.Application.Commands.ValueTypes.UpdateValueTypeDefinition;

internal sealed class UpdateValueTypeDefinitionValidator : AbstractValidator<UpdateValueTypeDefinitionCommand>
{
    public UpdateValueTypeDefinitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ValueTypeDefinition.MaxNameLength);
        RuleFor(x => x.Description)
            .MaximumLength(ValueTypeDefinition.MaxDescriptionLength)
            .When(x => x.Description is not null);
    }
}