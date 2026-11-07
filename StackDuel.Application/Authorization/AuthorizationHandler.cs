namespace StackDuel.Application.Authorization;

public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
    where TRequirement : IAuthorizationRequirement
{
    public async Task HandleAsync(AuthorizationContext context)
    {
        foreach (TRequirement requirement in context.PendingRequirements.OfType<TRequirement>())
        {
            if (await IsSatisfiedAsync(requirement, context.CancellationToken))
                context.Succeed(requirement);
        }
    }

    protected abstract Task<bool> IsSatisfiedAsync(TRequirement requirement, CancellationToken cancellationToken);
}
