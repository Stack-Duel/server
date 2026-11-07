namespace StackDuel.Application.Authorization;

public sealed class AuthorizationContext(IReadOnlyList<IAuthorizationRequirement> requirements, CancellationToken cancellationToken)
{
    private readonly HashSet<IAuthorizationRequirement> _succeeded = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<IAuthorizationRequirement> PendingRequirements { get; } = requirements;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public void Succeed(IAuthorizationRequirement requirement) => _succeeded.Add(requirement);

    public bool AllSucceeded => PendingRequirements.All(_succeeded.Contains);
}
