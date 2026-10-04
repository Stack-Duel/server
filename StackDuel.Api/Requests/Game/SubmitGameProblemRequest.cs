namespace StackDuel.Api.Requests.Game;

public sealed record SubmitGameProblemRequest(Guid ProblemSetupId, string Code);