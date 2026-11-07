using StackDuel.Application.Authorization;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Problems;

internal sealed class ProblemOwnershipPolicy(IProblemReadRepository problemReadRepository) : IOwnershipPolicy<Problem>
{
    public async Task<bool> IsOwnedByAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken)
    {
        Guid? createdById = await problemReadRepository.GetCreatedByIdAsync(resourceId, cancellationToken);
        return createdById == userId;
    }
}
