using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Submissions.Events;
using MediatR;

namespace StackDuel.Application.Events.Submissions;

internal sealed class SubmissionCreatedDomainEventHandler(IMessagePublisher messagePublisher)
    : INotificationHandler<DomainEventNotification<SubmissionCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SubmissionCreatedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        await messagePublisher.PublishAsync(
            new SubmissionCreatedMessage(notification.DomainEvent.SubmissionId),
            cancellationToken
        );
    }
}