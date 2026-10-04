using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignDetails;

internal sealed class UpdateCampaignDetailsValidator : AbstractValidator<UpdateCampaignDetailsCommand>
{
    public UpdateCampaignDetailsValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(4000);
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.IconKey).MaximumLength(200);
    }
}