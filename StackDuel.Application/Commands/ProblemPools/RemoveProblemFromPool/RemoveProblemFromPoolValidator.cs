using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.RemoveProblemFromPool;

internal sealed class RemoveProblemFromPoolValidator : AbstractValidator<RemoveProblemFromPoolCommand>
{
    public RemoveProblemFromPoolValidator()
    {
        RuleFor(x => x.PoolKey).NotEmpty();
        RuleFor(x => x.ProblemId).NotEmpty();
    }
}