namespace StackDuel.Application.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
