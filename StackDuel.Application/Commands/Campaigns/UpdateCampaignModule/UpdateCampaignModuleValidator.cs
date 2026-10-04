using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignModule;

internal sealed class UpdateCampaignModuleValidator : AbstractValidator<UpdateCampaignModuleCommand>
{
    public UpdateCampaignModuleValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(4000);
    }
}