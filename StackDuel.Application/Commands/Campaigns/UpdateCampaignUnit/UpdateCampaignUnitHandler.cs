using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignUnit;

internal sealed class UpdateCampaignUnitHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<UpdateCampaignUnitCommand> validator
) : AbstractCommandHandler<UpdateCampaignUnitCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateCampaignUnitCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        CampaignModule? module = campaign.Modules.FirstOrDefault(m => m.Id == request.ModuleId);

        if (module is null)
            return Result.NotFound();

        CampaignUnit? unit = module.Units.FirstOrDefault(u => u.Id == request.UnitId);

        if (unit is null)
            return Result.NotFound();

        unit.UpdateDetails(request.Title, request.Content, request.UnitType, request.EstimatedMinutes);

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}