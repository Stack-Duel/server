namespace StackDuel.Api.Requests.Submission;

public sealed record CreateGradeSubmissionRequest(
    Guid ProblemSetupId,
    string Code,
    IReadOnlyCollection<SubmissionFileRequest>? AdditionalFiles = null
);