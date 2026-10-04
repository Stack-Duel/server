using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Api.Requests.Submission;

public sealed record CreateSubmissionRequest(Guid ProblemSetupId, SubmissionType Type, string Code);