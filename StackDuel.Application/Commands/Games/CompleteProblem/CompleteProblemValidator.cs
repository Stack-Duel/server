using FluentValidation;

namespace StackDuel.Application.Commands.Games.CompleteProblem;

internal sealed class CompleteProblemValidator : AbstractValidator<CompleteProblemCommand>
{
    public CompleteProblemValidator()
    {
        RuleFor(x => x.GameId).NotEmpty().WithMessage("Game ID is required.");
        RuleFor(x => x.ProblemId).NotEmpty().WithMessage("Problem ID is required.");
        RuleFor(x => x.SubmissionId).NotEmpty().WithMessage("Submission ID is required.");
        RuleFor(x => x.RequestedByUserId).NotEmpty().WithMessage("User ID is required.");
    }
}