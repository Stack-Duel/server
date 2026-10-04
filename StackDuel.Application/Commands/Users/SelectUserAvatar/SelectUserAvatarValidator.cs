using FluentValidation;

namespace StackDuel.Application.Commands.Users.SelectUserAvatar;

internal sealed class SelectUserAvatarValidator : AbstractValidator<SelectUserAvatarCommand>
{
    public SelectUserAvatarValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AvatarId).NotEmpty();
    }
}