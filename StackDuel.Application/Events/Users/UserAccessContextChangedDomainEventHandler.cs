using StackDuel.Application.Services.Users;
using StackDuel.Domain.Users.Events;
using MediatR;

namespace StackDuel.Application.Events.Users;

internal sealed class UserAccessContextChangedDomainEventHandler(IUserService userService)
    : INotificationHandler<DomainEventNotification<UserAccessContextChangedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<UserAccessContextChangedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        userService.InvalidateAccessContext(notification.DomainEvent.Sub);
        return Task.CompletedTask;
    }
}