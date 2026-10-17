using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Events;

namespace StackDuel.Application.Commands.Users.UpdateProfilePrivacy;

internal sealed partial class UpdateProfilePrivacyHandler(
    IValidator<UpdateProfilePrivacyCommand> validator,
    IUserWriteRepository userWriteRepository,
    IDomainEventDispatcher domainEventDispatcher
) : AbstractCommandHandler<UpdateProfilePrivacyCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateProfilePrivacyCommand request,
        CancellationToken cancellationToken
    )
    {
        User? user = await userWriteRepository.FindByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound();

        user.SetPrivate(request.IsPrivate);
        await userWriteRepository.UpdateAsync(user, cancellationToken);

        await domainEventDispatcher.DispatchAsync(
            [new UserAccessContextChangedDomainEvent(user.Sub)],
            cancellationToken
        );

        return Result.Success();
    }
}