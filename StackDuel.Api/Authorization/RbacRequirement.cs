using Microsoft.AspNetCore.Authorization;

namespace StackDuel.Api.Authorization;

public sealed class RbacRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission ?? throw new ArgumentNullException(nameof(permission));
}