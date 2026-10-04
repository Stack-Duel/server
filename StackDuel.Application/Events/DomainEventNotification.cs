using MediatR;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Events;

public sealed record DomainEventNotification<T>(T DomainEvent) : INotification
    where T : IDomainEvent;