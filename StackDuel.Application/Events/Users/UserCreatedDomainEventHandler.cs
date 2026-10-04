using MediatR;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Events;

namespace StackDuel.Application.Events.Users;

internal sealed class UserCreatedDomainEventHandler(IUserWriteRepository userWriteRepository)
    : INotificationHandler<DomainEventNotification<UserCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserCreatedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        await userWriteRepository.AddToGroupAsync(
            notification.DomainEvent.UserId,
            WellKnownAuthorization.DefaultUserGroup,
            cancellationToken
        );
    }
}