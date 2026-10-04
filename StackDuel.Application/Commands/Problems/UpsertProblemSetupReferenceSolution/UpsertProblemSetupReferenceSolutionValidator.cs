using FluentValidation;

namespace StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;

internal sealed class UpsertProblemSetupReferenceSolutionValidator
    : AbstractValidator<UpsertProblemSetupReferenceSolutionCommand>
{
    public UpsertProblemSetupReferenceSolutionValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.LanguageVersionId).NotEmpty();
        RuleFor(x => x.InitialCode).NotEmpty().MaximumLength(20_000);
        RuleFor(x => x.FunctionName).MaximumLength(200).When(x => x.FunctionName is not null);
        RuleFor(x => x.ReferenceSolutionCode).NotEmpty().MaximumLength(20_000);
    }
}