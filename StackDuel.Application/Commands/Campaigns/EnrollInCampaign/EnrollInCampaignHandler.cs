using StackDuel.Application.Campaigns;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.EnrollInCampaign;

internal sealed class EnrollInCampaignHandler(
    ICampaignReadRepository campaignReadRepository,
    ICampaignProgressReadRepository campaignProgressReadRepository,
    ICampaignEnrollmentWriteRepository campaignEnrollmentWriteRepository,
    IValidator<EnrollInCampaignCommand> validator
) : AbstractCommandHandler<EnrollInCampaignCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        EnrollInCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignReadRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result<Guid>.NotFound();

        if (campaign.Status != CampaignStatus.Published)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(campaign.Status), "Only published campaigns can be enrolled in.")
            );

        CampaignEnrollment? existingEnrollment = await campaignProgressReadRepository.FindEnrollmentAsync(
            request.UserId,
            request.CampaignId,
            cancellationToken
        );

        if (existingEnrollment is not null)
            return Result<Guid>.Success(existingEnrollment.Id);

        CampaignEnrollment enrollment = new(request.UserId, request.CampaignId);

        await campaignEnrollmentWriteRepository.AddAsync(enrollment, cancellationToken);

        return Result<Guid>.Success(enrollment.Id);
    }
}