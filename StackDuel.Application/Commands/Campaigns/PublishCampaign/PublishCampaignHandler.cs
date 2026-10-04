using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.PublishCampaign;

internal sealed class PublishCampaignHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<PublishCampaignCommand> validator
) : AbstractCommandHandler<PublishCampaignCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        PublishCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        try
        {
            campaign.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Invalid(new ValidationError(nameof(campaign.Status), ex.Message));
        }

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}