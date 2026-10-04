using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;
using FluentValidation;

namespace StackDuel.Application.Commands.Submissions.CreateSubmission;

internal sealed class CreateSubmissionValidator : AbstractValidator<CreateSubmissionCommand>
{
    public CreateSubmissionValidator()
    {
        RuleFor(x => x.ProblemSetupId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(SourceCode.MaxLength);
        RuleFor(x => x.CreatedById).NotEmpty();

        RuleForEach(x => x.CustomTestCases!)
            .Must(tc => tc.Inputs.Count > 0)
            .WithMessage("Custom test case inputs must contain at least one value.")
            .When(x => x.Type == SubmissionType.Run && x.CustomTestCases is not null);
    }
}