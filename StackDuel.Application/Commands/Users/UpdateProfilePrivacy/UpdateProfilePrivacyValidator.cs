using FluentValidation;

namespace StackDuel.Application.Commands.Users.UpdateProfilePrivacy;

internal sealed class UpdateProfilePrivacyValidator : AbstractValidator<UpdateProfilePrivacyCommand>
{
    public UpdateProfilePrivacyValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}