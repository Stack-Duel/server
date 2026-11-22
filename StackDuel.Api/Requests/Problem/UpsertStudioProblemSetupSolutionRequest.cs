using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem;

public sealed record UpsertStudioProblemSetupSolutionRequest(
    [property: JsonRequired] Guid LanguageVersionId,
    string StarterCode,
    string? FunctionName,
    string ReferenceSolution
);