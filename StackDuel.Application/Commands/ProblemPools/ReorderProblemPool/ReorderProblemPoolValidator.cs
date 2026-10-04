using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.ReorderProblemPool;

internal sealed class ReorderProblemPoolValidator : AbstractValidator<ReorderProblemPoolCommand>
{
    public ReorderProblemPoolValidator()
    {
        RuleFor(x => x.PoolKey).NotEmpty();
        RuleFor(x => x.ProblemIds).NotEmpty();
    }
}