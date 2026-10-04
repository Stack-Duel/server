using FluentValidation;

namespace StackDuel.Application.Commands.Users.UpdateUserGroups;

internal sealed class UpdateUserGroupsValidator : AbstractValidator<UpdateUserGroupsCommand>
{
    public UpdateUserGroupsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GroupIds).NotNull();
        RuleForEach(x => x.GroupIds).NotEmpty();
    }
}