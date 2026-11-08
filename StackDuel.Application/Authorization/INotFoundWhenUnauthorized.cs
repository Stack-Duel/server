using Ardalis.Result;

namespace StackDuel.Application.Authorization;

public interface INotFoundWhenUnauthorized<TDto> : IAuthorizationGuarded<Result<TDto>>
{
    Result<TDto> IAuthorizationGuarded<Result<TDto>>.Unauthorized => Result<TDto>.NotFound();
}