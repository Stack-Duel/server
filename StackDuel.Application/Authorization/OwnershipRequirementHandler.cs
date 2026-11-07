using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Authorization;

internal sealed class OwnershipRequirementHandler<TEntity>(IOwnershipPolicy<TEntity> ownershipPolicy)
    : AuthorizationHandler<OwnershipRequirement<TEntity>>
    where TEntity : AggregateRoot
{
    protected override Task<bool> IsSatisfiedAsync(
        OwnershipRequirement<TEntity> requirement,
        CancellationToken cancellationToken
    ) => ownershipPolicy.IsOwnedByAsync(requirement.ResourceId, requirement.UserId, cancellationToken);
}
