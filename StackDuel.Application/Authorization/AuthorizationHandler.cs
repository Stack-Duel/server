namespace StackDuel.Application.Authorization;

public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
    where TRequirement : IAuthorizationRequirement
{
    public async Task HandleAsync(AuthorizationContext context)
    {
        foreach (IAuthorizationRequirement requirement in context.PendingRequirements)
        {
            if (requirement is not TRequirement typed)
                continue;

            if (await IsSatisfiedAsync(typed, context.CancellationToken))
                context.Succeed(requirement);
        }
    }

    protected abstract Task<bool> IsSatisfiedAsync(TRequirement requirement, CancellationToken cancellationToken);
}