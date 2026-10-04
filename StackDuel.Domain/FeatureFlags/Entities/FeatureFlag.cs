using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.FeatureFlags.Entities;

public sealed class FeatureFlag
{
    internal FeatureFlag(
        FeatureFlagId id,
        FeatureFlagKey key,
        string name,
        string description,
        bool defaultEnabled,
        RolloutPercentage rolloutPercentage,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required.", nameof(name));
        if (description is null)
            throw new ArgumentException("Description required.", nameof(description));

        Id = id;
        Key = key;
        Name = name;
        Description = description;
        DefaultEnabled = defaultEnabled;
        RolloutPercentage = rolloutPercentage;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static FeatureFlag Create(FeatureFlagKey key, string name, string description, bool defaultEnabled)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new FeatureFlag(
            new FeatureFlagId(Guid.NewGuid()),
            key,
            name,
            description,
            defaultEnabled,
            RolloutPercentage.Zero,
            now,
            now
        );
    }

    public void UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required.", nameof(name));
        if (description is null)
            throw new ArgumentException("Description required.", nameof(description));

        Name = name;
        Description = description;
        Touch();
    }

    public void SetDefaultEnabled(bool enabled)
    {
        DefaultEnabled = enabled;
        Touch();
    }

    public void SetRolloutPercentage(RolloutPercentage percentage)
    {
        RolloutPercentage = percentage;
        Touch();
    }

    public void SetUserOverride(UserId userId, DecisionEffect effect)
    {
        FeatureFlagUserOverride? existing = _userOverrides.FirstOrDefault(o => o.UserId == userId);
        if (existing is not null)
        {
            existing.UpdateEffect(effect);
        }
        else
        {
            _userOverrides.Add(new FeatureFlagUserOverride(userId, effect));
        }

        Touch();
    }

    public void RemoveUserOverride(UserId userId)
    {
        _userOverrides.RemoveWhere(o => o.UserId == userId);
        Touch();
    }

    public void SetGroupOverride(GroupId groupId, DecisionEffect effect)
    {
        FeatureFlagGroupOverride? existing = _groupOverrides.FirstOrDefault(o => o.GroupId == groupId);
        if (existing is not null)
        {
            existing.UpdateEffect(effect);
        }
        else
        {
            _groupOverrides.Add(new FeatureFlagGroupOverride(groupId, effect));
        }

        Touch();
    }

    public void RemoveGroupOverride(GroupId groupId)
    {
        _groupOverrides.RemoveWhere(o => o.GroupId == groupId);
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public FeatureFlagId Id { get; }

    public FeatureFlagKey Key { get; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public bool DefaultEnabled { get; private set; }

    public RolloutPercentage RolloutPercentage { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<FeatureFlagUserOverride> UserOverrides => _userOverrides.ToList().AsReadOnly();

    public IReadOnlyCollection<FeatureFlagGroupOverride> GroupOverrides => _groupOverrides.ToList().AsReadOnly();

    private readonly HashSet<FeatureFlagUserOverride> _userOverrides = [];
    private readonly HashSet<FeatureFlagGroupOverride> _groupOverrides = [];
}

public sealed class FeatureFlagUserOverride
{
    internal FeatureFlagUserOverride(UserId userId, DecisionEffect effect)
    {
        UserId = userId;
        Effect = effect;
    }

    public UserId UserId { get; }

    public DecisionEffect Effect { get; private set; }

    internal void UpdateEffect(DecisionEffect effect) => Effect = effect;
}

public sealed class FeatureFlagGroupOverride
{
    internal FeatureFlagGroupOverride(GroupId groupId, DecisionEffect effect)
    {
        GroupId = groupId;
        Effect = effect;
    }

    public GroupId GroupId { get; }

    public DecisionEffect Effect { get; private set; }

    internal void UpdateEffect(DecisionEffect effect) => Effect = effect;
}