namespace StackDuel.Application.Authorization;

internal sealed class PermissionRequirementHandler(UserContext userContext) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task<bool> IsSatisfiedAsync(PermissionRequirement requirement, CancellationToken cancellationToken) =>
        Task.FromResult(userContext.HasPermission(requirement.Permission));
}