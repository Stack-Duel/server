using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignDetails;

internal sealed class UpdateCampaignDetailsHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<UpdateCampaignDetailsCommand> validator
) : AbstractCommandHandler<UpdateCampaignDetailsCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateCampaignDetailsCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        campaign.UpdateDetails(request.Title, request.Description, request.Difficulty, request.IconKey);

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}