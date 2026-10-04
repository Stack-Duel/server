using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.AddCampaignModule;

internal sealed class AddCampaignModuleValidator : AbstractValidator<AddCampaignModuleCommand>
{
    public AddCampaignModuleValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(4000);
    }
}