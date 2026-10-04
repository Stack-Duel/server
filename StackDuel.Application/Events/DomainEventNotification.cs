using StackDuel.Domain.SeedWork;
using MediatR;

namespace StackDuel.Application.Events;

public sealed record DomainEventNotification<T>(T DomainEvent) : INotification
    where T : IDomainEvent;