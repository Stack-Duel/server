using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagUserOverride;

internal sealed class SetFeatureFlagUserOverrideHandler(
    IValidator<SetFeatureFlagUserOverrideCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<SetFeatureFlagUserOverrideCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetFeatureFlagUserOverrideCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? flag = await writeRepository.FindByIdAsync(request.FlagId, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        flag.SetUserOverride(new UserId(request.UserId), request.Effect);
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}