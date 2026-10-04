using StackDuel.Domain.Problems.Entities;
using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.CreateProblemPool;

internal sealed class CreateProblemPoolValidator : AbstractValidator<CreateProblemPoolCommand>
{
    public CreateProblemPoolValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(ProblemPool.MaxKeyLength);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(ProblemPool.MaxNameLength);
    }
}