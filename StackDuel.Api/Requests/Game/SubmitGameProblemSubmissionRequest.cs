namespace StackDuel.Api.Requests.Game;

public sealed record SubmitGameProblemSubmissionRequest(Guid ProblemSetupId, string Code);
