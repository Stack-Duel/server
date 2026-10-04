using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag;

internal sealed class CreateFeatureFlagHandler(
    IValidator<CreateFeatureFlagCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<CreateFeatureFlagCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateFeatureFlagCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? existing = await writeRepository.FindByKeyAsync(request.Key, cancellationToken);

        if (existing is not null)
            return Result.Invalid(
                new ValidationError("Key", $"A feature flag with key '{request.Key}' already exists.")
            );

        FeatureFlag flag = FeatureFlag.Create(
            new FeatureFlagKey(request.Key),
            request.Name,
            request.Description,
            request.DefaultEnabled
        );

        await writeRepository.AddAsync(flag, cancellationToken);
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(flag.Id.Value);
    }
}