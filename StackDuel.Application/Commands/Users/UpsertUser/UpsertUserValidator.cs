using FluentValidation;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Application.Commands.Users.UpsertUser;

internal sealed class UpsertUserValidator : AbstractValidator<UpsertUserCommand>
{
    public UpsertUserValidator()
    {
        RuleFor(x => x.Username).MaximumLength(Username.MaxLength).When(x => x.Username is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(ImageUrl.MaxLength);
        RuleFor(x => x.Bio).MaximumLength(Bio.MaxLength);
        RuleFor(x => x.LanguageIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Language preferences cannot contain duplicates.");
    }
}