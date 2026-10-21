namespace StackDuel.Api.Requests.Problem;

public sealed record ProblemSetupRequestItem(
    Guid? LanguageVersionId,
    string? Language,
    string? Version,
    string InitialCode,
    string? FunctionName,
    string ReferenceSolutionCode
);

public sealed record SetProblemSetupsRequest(IReadOnlyList<ProblemSetupRequestItem> Setups);