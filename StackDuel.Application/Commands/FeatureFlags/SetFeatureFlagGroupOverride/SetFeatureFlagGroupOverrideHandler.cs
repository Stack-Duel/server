using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagGroupOverride;

internal sealed class SetFeatureFlagGroupOverrideHandler(
    IValidator<SetFeatureFlagGroupOverrideCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<SetFeatureFlagGroupOverrideCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetFeatureFlagGroupOverrideCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? flag = await writeRepository.FindByIdAsync(request.FlagId, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        flag.SetGroupOverride(new GroupId(request.GroupId), request.Effect);
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}