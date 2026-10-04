using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.EnrollInCampaign;

internal sealed class EnrollInCampaignValidator : AbstractValidator<EnrollInCampaignCommand>
{
    public EnrollInCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}