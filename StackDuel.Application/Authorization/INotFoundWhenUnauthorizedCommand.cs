using Ardalis.Result;

namespace StackDuel.Application.Authorization;

public interface INotFoundWhenUnauthorizedCommand : IAuthorizationGuarded<Result>
{
    Result IAuthorizationGuarded<Result>.Unauthorized => Result.NotFound();
}
