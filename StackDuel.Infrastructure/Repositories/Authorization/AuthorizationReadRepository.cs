using StackDuel.Domain.Authorization;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Infrastructure.Persistence.Read;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Authorization;

internal class AuthorizationReadRepository(StackDuelReadDbContext context) : IAuthorizationReadRepository
{
    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<(string Code, DecisionEffect Effect)> restrictionRows = await context
            .SecurityRestrictions.Where(r => r.UserId == userId && r.ExpiresAt >= now)
            .Select(r => new ValueTuple<string, DecisionEffect>(r.PermissionCode, r.Effect))
            .ToListAsync(cancellationToken);

        List<(string Code, DecisionEffect Effect)> roleRows = await (
            from userGroup in context.UserGroups
            join groupRole in context.GroupRoles on userGroup.GroupId equals groupRole.GroupId
            join rolePermission in context.RolePermissions on groupRole.RoleId equals rolePermission.RoleId
            join permission in context.Permissions on rolePermission.PermissionId equals permission.Id
            where userGroup.UserId == userId
            select new ValueTuple<string, DecisionEffect>(permission.Code, rolePermission.Effect)
        ).ToListAsync(cancellationToken);

        Dictionary<string, HashSet<DecisionEffect>> restrictionsByCode = restrictionRows
            .GroupBy(r => r.Code)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Effect).ToHashSet());

        Dictionary<string, HashSet<DecisionEffect>> roleEffectsByCode = roleRows
            .GroupBy(r => r.Code)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Effect).ToHashSet());

        IEnumerable<string> allCodes = restrictionsByCode.Keys.Union(roleEffectsByCode.Keys);

        return allCodes.Where(code => HasAccess(code, restrictionsByCode, roleEffectsByCode)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetUserRoleNamesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await (
            from userGroup in context.UserGroups
            join groupRole in context.GroupRoles on userGroup.GroupId equals groupRole.GroupId
            join role in context.Roles on groupRole.RoleId equals role.Id
            where userGroup.UserId == userId
            select role.Name
        )
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static bool HasAccess(
        string code,
        Dictionary<string, HashSet<DecisionEffect>> restrictionsByCode,
        Dictionary<string, HashSet<DecisionEffect>> roleEffectsByCode
    )
    {
        if (restrictionsByCode.TryGetValue(code, out var restrictionEffects))
        {
            if (restrictionEffects.Contains(DecisionEffect.Deny))
                return false;

            if (restrictionEffects.Contains(DecisionEffect.Allow))
                return true;
        }

        if (roleEffectsByCode.TryGetValue(code, out var roleEffects))
        {
            if (roleEffects.Contains(DecisionEffect.Deny))
                return false;

            return roleEffects.Contains(DecisionEffect.Allow);
        }

        return false;
    }
}