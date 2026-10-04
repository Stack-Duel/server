using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.PublishCampaign;

internal sealed class PublishCampaignValidator : AbstractValidator<PublishCampaignCommand>
{
    public PublishCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}