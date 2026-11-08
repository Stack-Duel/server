using MediatR;
using StackDuel.Application.Authorization;

namespace StackDuel.Application.Behaviors;

internal sealed class AuthorizationBehavior<TRequest, TResponse>(IEnumerable<IAuthorizationHandler> handlers)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is not IAuthorizationGuarded<TResponse> guarded)
            return await next(cancellationToken);

        var context = new AuthorizationContext(guarded.Requirements, cancellationToken);

        foreach (IAuthorizationHandler handler in handlers)
        {
            await handler.HandleAsync(context);
        }

        return context.AllSucceeded ? await next(cancellationToken) : guarded.Unauthorized;
    }
}