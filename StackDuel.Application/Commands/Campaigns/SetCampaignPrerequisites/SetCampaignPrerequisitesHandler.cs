using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Campaigns;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Commands.Campaigns.SetCampaignPrerequisites;

internal sealed class SetCampaignPrerequisitesHandler(
    ICampaignReadRepository campaignReadRepository,
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<SetCampaignPrerequisitesCommand> validator
) : AbstractCommandHandler<SetCampaignPrerequisitesCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetCampaignPrerequisitesCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        foreach (Guid requiredCampaignId in request.RequiredCampaignIds.Distinct())
        {
            bool exists = await campaignReadRepository.ExistsAsync(requiredCampaignId, cancellationToken);
            if (!exists)
                return Result.Invalid(
                    new ValidationError(
                        nameof(request.RequiredCampaignIds),
                        $"Campaign '{requiredCampaignId}' was not found."
                    )
                );
        }

        try
        {
            campaign.SetPrerequisites(request.RequiredCampaignIds);
        }
        catch (ArgumentException ex)
        {
            return Result.Invalid(new ValidationError(nameof(request.RequiredCampaignIds), ex.Message));
        }

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}