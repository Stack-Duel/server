using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.AddCampaignUnit;

internal sealed class AddCampaignUnitValidator : AbstractValidator<AddCampaignUnitCommand>
{
    public AddCampaignUnitValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotNull().MaximumLength(20000);
        RuleFor(x => x.UnitType).IsInEnum();
        RuleFor(x => x.EstimatedMinutes).GreaterThanOrEqualTo(0);
    }
}