using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag;
using StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagGroupOverride;
using StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagUserOverride;
using StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagGroupOverride;
using StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagUserOverride;
using StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagDefault;
using StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout;
using StackDuel.Application.FeatureFlags.Dtos;
using StackDuel.Application.Queries.FeatureFlags.GetFeatureFlagByKey;
using StackDuel.Application.Queries.FeatureFlags.GetFeatureFlags;
using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Application.Services.FeatureFlags;

public interface IFeatureFlagAdminService
{
    Task<Result<IReadOnlyList<FeatureFlagAdminDto>>> GetAllAsync(CancellationToken cancellationToken);

    Task<Result<FeatureFlagAdminDto>> GetByKeyAsync(string key, CancellationToken cancellationToken);

    Task<Result<Guid>> CreateAsync(
        string key,
        string name,
        string description,
        bool defaultEnabled,
        CancellationToken cancellationToken
    );

    Task<Result> UpdateDefaultAsync(Guid flagId, bool defaultEnabled, CancellationToken cancellationToken);

    Task<Result> UpdateRolloutAsync(Guid flagId, int rolloutPercentage, CancellationToken cancellationToken);

    Task<Result> SetUserOverrideAsync(
        Guid flagId,
        Guid userId,
        DecisionEffect effect,
        CancellationToken cancellationToken
    );

    Task<Result> RemoveUserOverrideAsync(Guid flagId, Guid userId, CancellationToken cancellationToken);

    Task<Result> SetGroupOverrideAsync(
        Guid flagId,
        Guid groupId,
        DecisionEffect effect,
        CancellationToken cancellationToken
    );

    Task<Result> RemoveGroupOverrideAsync(Guid flagId, Guid groupId, CancellationToken cancellationToken);
}

internal sealed class FeatureFlagAdminService(IMediator mediator) : IFeatureFlagAdminService
{
    public async Task<Result<IReadOnlyList<FeatureFlagAdminDto>>> GetAllAsync(CancellationToken cancellationToken) =>
        await mediator.Send(new GetFeatureFlagsQuery(), cancellationToken);

    public async Task<Result<FeatureFlagAdminDto>> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        await mediator.Send(new GetFeatureFlagByKeyQuery(key), cancellationToken);

    public async Task<Result<Guid>> CreateAsync(
        string key,
        string name,
        string description,
        bool defaultEnabled,
        CancellationToken cancellationToken
    ) => await mediator.Send(new CreateFeatureFlagCommand(key, name, description, defaultEnabled), cancellationToken);

    public async Task<Result> UpdateDefaultAsync(
        Guid flagId,
        bool defaultEnabled,
        CancellationToken cancellationToken
    ) => await mediator.Send(new UpdateFeatureFlagDefaultCommand(flagId, defaultEnabled), cancellationToken);

    public async Task<Result> UpdateRolloutAsync(
        Guid flagId,
        int rolloutPercentage,
        CancellationToken cancellationToken
    ) => await mediator.Send(new UpdateFeatureFlagRolloutCommand(flagId, rolloutPercentage), cancellationToken);

    public async Task<Result> SetUserOverrideAsync(
        Guid flagId,
        Guid userId,
        DecisionEffect effect,
        CancellationToken cancellationToken
    ) => await mediator.Send(new SetFeatureFlagUserOverrideCommand(flagId, userId, effect), cancellationToken);

    public async Task<Result> RemoveUserOverrideAsync(Guid flagId, Guid userId, CancellationToken cancellationToken) =>
        await mediator.Send(new RemoveFeatureFlagUserOverrideCommand(flagId, userId), cancellationToken);

    public async Task<Result> SetGroupOverrideAsync(
        Guid flagId,
        Guid groupId,
        DecisionEffect effect,
        CancellationToken cancellationToken
    ) => await mediator.Send(new SetFeatureFlagGroupOverrideCommand(flagId, groupId, effect), cancellationToken);

    public async Task<Result> RemoveGroupOverrideAsync(
        Guid flagId,
        Guid groupId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new RemoveFeatureFlagGroupOverrideCommand(flagId, groupId), cancellationToken);
}