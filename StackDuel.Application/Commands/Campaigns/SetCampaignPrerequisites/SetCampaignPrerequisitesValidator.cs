using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.SetCampaignPrerequisites;

internal sealed class SetCampaignPrerequisitesValidator : AbstractValidator<SetCampaignPrerequisitesCommand>
{
    public SetCampaignPrerequisitesValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.RequiredCampaignIds).NotNull();
    }
}