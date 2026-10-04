using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagUserOverride;

internal sealed class RemoveFeatureFlagUserOverrideHandler(
    IValidator<RemoveFeatureFlagUserOverrideCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<RemoveFeatureFlagUserOverrideCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        RemoveFeatureFlagUserOverrideCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? flag = await writeRepository.FindByIdAsync(request.FlagId, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        flag.RemoveUserOverride(new UserId(request.UserId));
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}