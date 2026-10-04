using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignUnit;

internal sealed class UpdateCampaignUnitValidator : AbstractValidator<UpdateCampaignUnitCommand>
{
    public UpdateCampaignUnitValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotNull().MaximumLength(20000);
        RuleFor(x => x.UnitType).IsInEnum();
        RuleFor(x => x.EstimatedMinutes).GreaterThanOrEqualTo(0);
    }
}