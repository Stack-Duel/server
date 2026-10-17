using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Submissions;

namespace StackDuel.Application.Commands.Submissions.CreateSubmission;

internal sealed class CreateSubmissionHandler(
    IValidator<CreateSubmissionCommand> validator,
    ISubmissionCreator submissionCreator
) : AbstractCommandHandler<CreateSubmissionCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateSubmissionCommand request,
        CancellationToken cancellationToken
    ) =>
        await submissionCreator.CreateAsync(
            new SubmissionCreationRequest(
                request.ProblemSetupId,
                request.Type,
                request.Code,
                request.CreatedById,
                request.CustomTestCases,
                request.AdditionalFiles,
                request.GameId
            ),
            cancellationToken
        );
}