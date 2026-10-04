using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.ArchiveCampaign;

internal sealed class ArchiveCampaignValidator : AbstractValidator<ArchiveCampaignCommand>
{
    public ArchiveCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}