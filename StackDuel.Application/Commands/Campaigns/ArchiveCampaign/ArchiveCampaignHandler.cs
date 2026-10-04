using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Commands.Campaigns.ArchiveCampaign;

internal sealed class ArchiveCampaignHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<ArchiveCampaignCommand> validator
) : AbstractCommandHandler<ArchiveCampaignCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        ArchiveCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        try
        {
            campaign.Archive();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Invalid(new ValidationError(nameof(campaign.Status), ex.Message));
        }

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}