using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;

namespace StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagGroupOverride;

internal sealed class RemoveFeatureFlagGroupOverrideHandler(
    IValidator<RemoveFeatureFlagGroupOverrideCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<RemoveFeatureFlagGroupOverrideCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        RemoveFeatureFlagGroupOverrideCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? flag = await writeRepository.FindByIdAsync(request.FlagId, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        flag.RemoveGroupOverride(new GroupId(request.GroupId));
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}