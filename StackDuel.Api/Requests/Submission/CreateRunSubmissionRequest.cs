namespace StackDuel.Api.Requests.Submission;

public sealed record RunTestCaseInputRequest(IReadOnlyCollection<string> Inputs);

public sealed record SubmissionFileRequest(string Path, string Content);

public sealed record CreateRunSubmissionRequest(
    Guid ProblemSetupId,
    string Code,
    IReadOnlyCollection<RunTestCaseInputRequest>? CustomTestCases,
    IReadOnlyCollection<SubmissionFileRequest>? AdditionalFiles = null
);