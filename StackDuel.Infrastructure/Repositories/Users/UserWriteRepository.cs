using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Users;

internal sealed class UserWriteRepository(StackDuelDbContext context) : IUserWriteRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddToGroupAsync(Guid userId, string groupName, CancellationToken cancellationToken = default)
    {
        Group? group = await context.Groups.FirstOrDefaultAsync(g => g.Name == new Name(groupName), cancellationToken);

        if (group is null)
            return;

        var joinEntity = context.Set<Dictionary<string, object>>("user_groups");

        bool alreadyAssigned = await joinEntity.AnyAsync(
            ug => EF.Property<Guid>(ug, "user_id") == userId && EF.Property<GroupId>(ug, "group_id") == group.Id,
            cancellationToken
        );

        if (alreadyAssigned)
            return;

        joinEntity.Add(new Dictionary<string, object> { ["user_id"] = userId, ["group_id"] = group.Id });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetGroupIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var joinEntity = context.Set<Dictionary<string, object>>("user_groups");

        List<Dictionary<string, object>> rows = await joinEntity
            .Where(ug => EF.Property<Guid>(ug, "user_id") == userId)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => ((GroupId)row["group_id"]).Value)];
    }

    public async Task SetGroupsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default
    )
    {
        var joinEntity = context.Set<Dictionary<string, object>>("user_groups");

        List<Dictionary<string, object>> currentRows = await joinEntity
            .Where(ug => EF.Property<Guid>(ug, "user_id") == userId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> desiredGroupIds = [.. groupIds];

        foreach (Dictionary<string, object> row in currentRows)
        {
            Guid groupId = ((GroupId)row["group_id"]).Value;

            if (desiredGroupIds.Remove(groupId))
                continue;

            joinEntity.Remove(row);
        }

        foreach (Guid groupId in desiredGroupIds)
        {
            joinEntity.Add(
                new Dictionary<string, object> { ["user_id"] = userId, ["group_id"] = new GroupId(groupId) }
            );
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetLanguagePreferencesAsync(
        Guid userId,
        IReadOnlyList<Guid> languageIdsInOrder,
        CancellationToken cancellationToken = default
    )
    {
        var joinEntity = context.Set<Dictionary<string, object>>("user_language_preferences");

        List<Dictionary<string, object>> currentRows = await joinEntity
            .Where(ulp => EF.Property<Guid>(ulp, "user_id") == userId)
            .ToListAsync(cancellationToken);

        foreach (Dictionary<string, object> row in currentRows)
            joinEntity.Remove(row);

        await context.SaveChangesAsync(cancellationToken);

        List<Guid> orderedLanguageIds = [.. languageIdsInOrder.Distinct()];

        for (int position = 0; position < orderedLanguageIds.Count; position++)
        {
            joinEntity.Add(
                new Dictionary<string, object>
                {
                    ["user_id"] = userId,
                    ["language_id"] = orderedLanguageIds[position],
                    ["position"] = position,
                }
            );
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> FindBySubAsync(string sub, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Sub == sub, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> FindByUsername(Username username, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }
}