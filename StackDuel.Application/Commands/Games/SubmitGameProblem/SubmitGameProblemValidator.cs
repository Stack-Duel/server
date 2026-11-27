using FluentValidation;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Application.Commands.Games.SubmitGameProblem;

internal sealed class SubmitGameProblemValidator : AbstractValidator<SubmitGameProblemCommand>
{
    public SubmitGameProblemValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.ProblemSetupId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(SourceCode.MaxLength);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}