using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagDefault;

internal sealed class UpdateFeatureFlagDefaultHandler(
    IValidator<UpdateFeatureFlagDefaultCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<UpdateFeatureFlagDefaultCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateFeatureFlagDefaultCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? flag = await writeRepository.FindByIdAsync(request.FlagId, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        flag.SetDefaultEnabled(request.DefaultEnabled);
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}