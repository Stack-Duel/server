using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.ValueObjects;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.FeatureFlags;

internal sealed class FeatureFlagEvaluationRepository(StackDuelDbContext context) : IFeatureFlagEvaluationRepository
{
    public async Task<FeatureFlagEvaluationData?> GetEvaluationDataAsync(
        string flagKey,
        Guid? userId,
        CancellationToken cancellationToken
    )
    {
        FeatureFlagKey key = new(flagKey);

        var flag = await context
            .FeatureFlags.Where(f => f.Key == key)
            .Select(f => new
            {
                f.Id,
                f.DefaultEnabled,
                RolloutPercentage = f.RolloutPercentage.Value,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (flag is null)
            return null;

        DecisionEffect? userEffect = null;
        DecisionEffect? groupEffect = null;

        if (userId is Guid uid)
        {
            UserId vUserId = new(uid);

            List<DecisionEffect> userRows = await context
                .FeatureFlagUserOverrides.Where(o =>
                    EF.Property<FeatureFlagId>(o, "feature_flag_id") == flag.Id && o.UserId == vUserId
                )
                .Select(o => o.Effect)
                .ToListAsync(cancellationToken);

            userEffect = Collapse(userRows);

            if (userEffect is null)
            {
                List<DecisionEffect> groupRows = await (
                    from go in context.FeatureFlagGroupOverrides
                    join ug in context.Set<Dictionary<string, object>>("user_groups")
                        on go.GroupId equals EF.Property<GroupId>(ug, "group_id")
                    where
                        EF.Property<Guid>(ug, "user_id") == uid
                        && EF.Property<FeatureFlagId>(go, "feature_flag_id") == flag.Id
                    select go.Effect
                ).ToListAsync(cancellationToken);

                groupEffect = Collapse(groupRows);
            }
        }

        return new FeatureFlagEvaluationData(
            flagKey,
            flag.DefaultEnabled,
            flag.RolloutPercentage,
            userEffect,
            groupEffect
        );
    }

    public async Task<IReadOnlyDictionary<string, FeatureFlagEvaluationData>> GetAllEvaluationDataAsync(
        Guid? userId,
        CancellationToken cancellationToken
    )
    {
        List<(FeatureFlagId Id, string Key, bool DefaultEnabled, int RolloutPercentage)> flags = await context
            .FeatureFlags.Select(f => new ValueTuple<FeatureFlagId, string, bool, int>(
                f.Id,
                f.Key.Value,
                f.DefaultEnabled,
                f.RolloutPercentage.Value
            ))
            .ToListAsync(cancellationToken);

        Dictionary<FeatureFlagId, HashSet<DecisionEffect>> userEffectsByFlag = [];
        Dictionary<FeatureFlagId, HashSet<DecisionEffect>> groupEffectsByFlag = [];

        if (userId is Guid uid)
        {
            UserId vUserId = new(uid);

            List<(FeatureFlagId FlagId, DecisionEffect Effect)> userRows = await context
                .FeatureFlagUserOverrides.Where(o => o.UserId == vUserId)
                .Select(o => new ValueTuple<FeatureFlagId, DecisionEffect>(
                    EF.Property<FeatureFlagId>(o, "feature_flag_id"),
                    o.Effect
                ))
                .ToListAsync(cancellationToken);

            userEffectsByFlag = userRows
                .GroupBy(r => r.FlagId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Effect).ToHashSet());

            List<(FeatureFlagId FlagId, DecisionEffect Effect)> groupRows = await (
                from go in context.FeatureFlagGroupOverrides
                join ug in context.Set<Dictionary<string, object>>("user_groups")
                    on go.GroupId equals EF.Property<GroupId>(ug, "group_id")
                where EF.Property<Guid>(ug, "user_id") == uid
                select new ValueTuple<FeatureFlagId, DecisionEffect>(
                    EF.Property<FeatureFlagId>(go, "feature_flag_id"),
                    go.Effect
                )
            ).ToListAsync(cancellationToken);

            groupEffectsByFlag = groupRows
                .GroupBy(r => r.FlagId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Effect).ToHashSet());
        }

        Dictionary<string, FeatureFlagEvaluationData> result = [];

        foreach (var flag in flags)
        {
            DecisionEffect? userEffect = userEffectsByFlag.TryGetValue(flag.Id, out var uEffects)
                ? Collapse(uEffects)
                : null;
            DecisionEffect? groupEffect = groupEffectsByFlag.TryGetValue(flag.Id, out var gEffects)
                ? Collapse(gEffects)
                : null;

            result[flag.Key] = new FeatureFlagEvaluationData(
                flag.Key,
                flag.DefaultEnabled,
                flag.RolloutPercentage,
                userEffect,
                groupEffect
            );
        }

        return result;
    }

    private static DecisionEffect? Collapse(IReadOnlyCollection<DecisionEffect> effects)
    {
        if (effects.Contains(DecisionEffect.Deny))
            return DecisionEffect.Deny;
        if (effects.Contains(DecisionEffect.Allow))
            return DecisionEffect.Allow;
        return null;
    }
}