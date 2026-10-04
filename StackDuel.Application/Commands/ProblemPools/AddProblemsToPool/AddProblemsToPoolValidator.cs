using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.AddProblemsToPool;

internal sealed class AddProblemsToPoolValidator : AbstractValidator<AddProblemsToPoolCommand>
{
    public AddProblemsToPoolValidator()
    {
        RuleFor(x => x.PoolKey).NotEmpty();
        RuleFor(x => x.ProblemIds).NotEmpty().When(x => !x.SelectAllMatching);
    }
}