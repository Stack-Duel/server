using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.AddProblemToPool;

internal sealed class AddProblemToPoolValidator : AbstractValidator<AddProblemToPoolCommand>
{
    public AddProblemToPoolValidator()
    {
        RuleFor(x => x.PoolKey).NotEmpty();
        RuleFor(x => x.ProblemId).NotEmpty();
    }
}