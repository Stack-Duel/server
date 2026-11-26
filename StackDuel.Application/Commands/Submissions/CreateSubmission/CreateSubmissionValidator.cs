using FluentValidation;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;
using System.Text.RegularExpressions;

namespace StackDuel.Application.Commands.Submissions.CreateSubmission;

internal sealed partial class CreateSubmissionValidator : AbstractValidator<CreateSubmissionCommand>
{
    public const int MaxCustomTestCases = 25;

    public const int MaxCustomTestCaseInputs = 20;

    public const int MaxCustomTestCaseInputLength = 4096;

    private static readonly IReadOnlySet<string> ReservedFileNames = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "harness",
        "solution",
        "run",
        "compile",
        "main",
        "index",
    };

    public CreateSubmissionValidator()
    {
        RuleFor(x => x.ProblemSetupId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(SourceCode.MaxLength);
        RuleFor(x => x.CreatedById).NotEmpty();

        RuleFor(x => x.AdditionalFiles!)
            .Must(files => files.Count <= SubmissionSourceFile.MaxFilesPerSubmission)
            .WithMessage($"A submission cannot include more than {SubmissionSourceFile.MaxFilesPerSubmission} files.")
            .Must(files => files.Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() == files.Count)
            .WithMessage("Submission file paths must be unique.")
            .When(x => x.AdditionalFiles is not null);

        RuleForEach(x => x.AdditionalFiles!)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.Path)
                    .NotEmpty()
                    .MaximumLength(SubmissionSourceFile.MaxPathLength)
                    .Must(path => AllowedFileName().IsMatch(path))
                    .WithMessage(
                        "File path may only contain letters, digits, underscores, hyphens, and a single extension."
                    )
                    .Must(path => !ReservedFileNames.Contains(NameWithoutExtension(path)))
                    .WithMessage("That file path is reserved by the execution runtime.");

                file.RuleFor(f => f.Content).MaximumLength(SubmissionSourceFile.MaxContentLength);
            })
            .When(x => x.AdditionalFiles is not null);

        RuleFor(x => x.CustomTestCases!)
            .Must(cases => cases.Count <= MaxCustomTestCases)
            .WithMessage($"A run cannot include more than {MaxCustomTestCases} custom test cases.")
            .When(x => x.Type == SubmissionType.Run && x.CustomTestCases is not null);

        RuleForEach(x => x.CustomTestCases!)
            .Must(tc => tc.Inputs.Count > 0)
            .WithMessage("Custom test case inputs must contain at least one value.")
            .Must(tc => tc.Inputs.Count <= MaxCustomTestCaseInputs)
            .WithMessage($"A custom test case cannot have more than {MaxCustomTestCaseInputs} inputs.")
            .Must(tc => tc.Inputs.All(input => input.Length <= MaxCustomTestCaseInputLength))
            .WithMessage($"A custom test case input cannot exceed {MaxCustomTestCaseInputLength} characters.")
            .When(x => x.Type == SubmissionType.Run && x.CustomTestCases is not null);
    }

    private static string NameWithoutExtension(string path)
    {
        int lastDot = path.LastIndexOf('.');
        return lastDot < 0 ? path : path[..lastDot];
    }

    [GeneratedRegex(@"^[\p{L}\p{Nd}_-]+(\.[\p{L}\p{Nd}]+)?$")]
    private static partial Regex AllowedFileName();
}