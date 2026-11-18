using StackDuel.Application.Authorization;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.Problems;

internal static class ProblemCommandAuthorization
{
    public static IReadOnlyList<IAuthorizationRequirement> OwnershipRequirements(
        Guid problemId,
        Guid? requestedByUserId
    ) =>
        requestedByUserId.HasValue
            ? [new OwnershipRequirement<Problem>(problemId, requestedByUserId.Value)]
            : [];
}