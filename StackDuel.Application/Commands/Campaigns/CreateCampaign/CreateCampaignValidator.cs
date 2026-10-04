using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.CreateCampaign;

internal sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(4000);
        RuleFor(x => x.Difficulty).IsInEnum();
    }
}