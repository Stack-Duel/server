using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Commands.Campaigns.AddCampaignModule;

internal sealed class AddCampaignModuleHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<AddCampaignModuleCommand> validator
) : AbstractCommandHandler<AddCampaignModuleCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        AddCampaignModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result<Guid>.NotFound();

        CampaignModule module = campaign.AddModule(request.Title, request.Description);

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result<Guid>.Success(module.Id);
    }
}