using FluentValidation;

namespace StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge;

internal sealed class UpdateDailyChallengeValidator : AbstractValidator<UpdateDailyChallengeCommand>
{
    public UpdateDailyChallengeValidator()
    {
        RuleFor(x => x.Date)
            .Must(date => date > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Only challenges scheduled after today can be updated.");

        RuleFor(x => x.ProblemId).NotEmpty();
    }
}