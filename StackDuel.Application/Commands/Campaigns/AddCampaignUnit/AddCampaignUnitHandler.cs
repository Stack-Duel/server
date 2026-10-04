using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.AddCampaignUnit;

internal sealed class AddCampaignUnitHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<AddCampaignUnitCommand> validator
) : AbstractCommandHandler<AddCampaignUnitCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        AddCampaignUnitCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result<Guid>.NotFound();

        CampaignModule? module = campaign.Modules.FirstOrDefault(m => m.Id == request.ModuleId);

        if (module is null)
            return Result<Guid>.NotFound();

        CampaignUnit unit = module.AddUnit(request.Title, request.Content, request.UnitType, request.EstimatedMinutes);

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result<Guid>.Success(unit.Id);
    }
}