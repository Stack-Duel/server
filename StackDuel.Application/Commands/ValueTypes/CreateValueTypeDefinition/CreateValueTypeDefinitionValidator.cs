using FluentValidation;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Application.Commands.ValueTypes.CreateValueTypeDefinition;

internal sealed class CreateValueTypeDefinitionValidator : AbstractValidator<CreateValueTypeDefinitionCommand>
{
    public CreateValueTypeDefinitionValidator()
    {
        RuleFor(x => x.Key).NotEmpty().Length(ValueTypeKey.MinLength, ValueTypeKey.MaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ValueTypeDefinition.MaxNameLength);
        RuleFor(x => x.Description)
            .MaximumLength(ValueTypeDefinition.MaxDescriptionLength)
            .When(x => x.Description is not null);
        RuleFor(x => x.Spec).NotNull();
    }
}