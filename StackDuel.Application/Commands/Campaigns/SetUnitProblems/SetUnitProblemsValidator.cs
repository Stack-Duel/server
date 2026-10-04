using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.SetUnitProblems;

internal sealed class SetUnitProblemsValidator : AbstractValidator<SetUnitProblemsCommand>
{
    public SetUnitProblemsValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.ProblemIds).NotNull();
    }
}