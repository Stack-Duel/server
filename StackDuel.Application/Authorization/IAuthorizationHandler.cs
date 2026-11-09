namespace StackDuel.Application.Authorization;

public interface IAuthorizationHandler
{
    Task HandleAsync(AuthorizationContext context);
}