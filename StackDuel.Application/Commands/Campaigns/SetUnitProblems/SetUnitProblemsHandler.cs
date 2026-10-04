using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Problems;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Commands.Campaigns.SetUnitProblems;

internal sealed class SetUnitProblemsHandler(
    ICampaignWriteRepository campaignWriteRepository,
    IProblemReadRepository problemReadRepository,
    IValidator<SetUnitProblemsCommand> validator
) : AbstractCommandHandler<SetUnitProblemsCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetUnitProblemsCommand request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignWriteRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        CampaignModule? module = campaign.Modules.FirstOrDefault(m => m.Id == request.ModuleId);

        if (module is null)
            return Result.NotFound();

        CampaignUnit? unit = module.Units.FirstOrDefault(u => u.Id == request.UnitId);

        if (unit is null)
            return Result.NotFound();

        foreach (Guid problemId in request.ProblemIds.Distinct())
        {
            bool exists = await problemReadRepository.ExistsForAdminAsync(problemId, cancellationToken);
            if (!exists)
                return Result.Invalid(
                    new ValidationError(nameof(request.ProblemIds), $"Problem '{problemId}' was not found.")
                );
        }

        unit.SetProblems(request.ProblemIds);

        await campaignWriteRepository.SaveChangesAsync(campaign, cancellationToken);

        return Result.Success();
    }
}