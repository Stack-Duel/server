using FluentValidation;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Application.Commands.Users.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(Username.MaxLength);
        RuleFor(x => x.Sub).NotEmpty();
        RuleFor(x => x.ImageUrl).MaximumLength(ImageUrl.MaxLength).When(x => x.ImageUrl is not null);
    }
}