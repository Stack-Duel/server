using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SetProblemReaction;

internal sealed class SetProblemReactionValidator : AbstractValidator<SetProblemReactionCommand>
{
    public SetProblemReactionValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ReactionTypeKey).NotEmpty();
    }
}