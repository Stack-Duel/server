namespace StackDuel.Application.Authorization;

public interface IAuthorizationGuarded<TResponse>
{
    IReadOnlyList<IAuthorizationRequirement> Requirements { get; }

    TResponse Unauthorized { get; }
}