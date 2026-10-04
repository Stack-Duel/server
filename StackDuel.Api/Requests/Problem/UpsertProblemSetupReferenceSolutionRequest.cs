using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem;

public sealed record UpsertProblemSetupReferenceSolutionRequest(
    [property: JsonRequired] Guid LanguageVersionId,
    string InitialCode,
    string? FunctionName,
    string ReferenceSolutionCode
);