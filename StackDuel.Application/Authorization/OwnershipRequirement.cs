using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Authorization;

public sealed record OwnershipRequirement<TEntity>(Guid ResourceId, Guid UserId) : IAuthorizationRequirement
    where TEntity : AggregateRoot;