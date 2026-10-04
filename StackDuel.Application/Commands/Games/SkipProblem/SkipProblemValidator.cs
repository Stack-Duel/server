using FluentValidation;

namespace StackDuel.Application.Commands.Games.SkipProblem;

internal sealed class SkipProblemValidator : AbstractValidator<SkipProblemCommand>
{
    public SkipProblemValidator()
    {
        RuleFor(x => x.GameId).NotEmpty().WithMessage("Game ID is required.");
        RuleFor(x => x.ProblemId).NotEmpty().WithMessage("Problem ID is required.");
        RuleFor(x => x.RequestedByUserId).NotEmpty().WithMessage("User ID is required.");
    }
}