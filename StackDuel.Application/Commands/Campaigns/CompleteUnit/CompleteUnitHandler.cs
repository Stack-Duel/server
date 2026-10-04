using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Campaigns;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Commands.Campaigns.CompleteUnit;

internal sealed class CompleteUnitHandler(
    ICampaignReadRepository campaignReadRepository,
    ICampaignProgressReadRepository campaignProgressReadRepository,
    ICampaignEnrollmentWriteRepository campaignEnrollmentWriteRepository,
    IUnitCompletionWriteRepository unitCompletionWriteRepository,
    IValidator<CompleteUnitCommand> validator
) : AbstractCommandHandler<CompleteUnitCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        CompleteUnitCommand request,
        CancellationToken cancellationToken
    )
    {
        bool alreadyCompleted = await campaignProgressReadRepository.HasCompletedUnitAsync(
            request.UserId,
            request.UnitId,
            cancellationToken
        );

        if (alreadyCompleted)
            return Result.Success();

        Campaign? campaign = await campaignReadRepository.FindByUnitIdAsync(request.UnitId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        UnitCompletion completion = new(request.UserId, request.UnitId);
        await unitCompletionWriteRepository.AddAsync(completion, cancellationToken);

        int totalUnits = campaign.Modules.SelectMany(m => m.Units).Count();

        IReadOnlyList<Guid> completedUnitIds = await campaignProgressReadRepository.GetCompletedUnitIdsAsync(
            request.UserId,
            campaign.Id,
            cancellationToken
        );

        if (totalUnits == 0 || completedUnitIds.Count < totalUnits)
            return Result.Success();

        CampaignEnrollment? enrollment = await campaignProgressReadRepository.FindEnrollmentAsync(
            request.UserId,
            campaign.Id,
            cancellationToken
        );

        if (enrollment is null)
        {
            enrollment = new CampaignEnrollment(request.UserId, campaign.Id);
            enrollment.Complete();
            await campaignEnrollmentWriteRepository.AddAsync(enrollment, cancellationToken);
        }
        else if (enrollment.Status != EnrollmentStatus.Completed)
        {
            enrollment.Complete();
            await campaignEnrollmentWriteRepository.SaveChangesAsync(enrollment, cancellationToken);
        }

        return Result.Success();
    }
}