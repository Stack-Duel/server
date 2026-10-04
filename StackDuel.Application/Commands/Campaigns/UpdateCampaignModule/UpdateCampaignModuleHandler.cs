using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignModule;

internal sealed class UpdateCampaignModuleHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<UpdateCampaignModuleCommand> validator
) : AbstractCommandHandler<UpdateCampaignModuleCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateCampaignModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        CampaignModule? module = campaign.Modules.FirstOrDefault(m => m.Id == request.ModuleId);

        if (module is null)
            return Result.NotFound();

        module.UpdateDetails(request.Title, request.Description);

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}