using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem.RequiredLanguages;

public sealed record AddRequiredProblemLanguageRequest([property: JsonRequired] Guid LanguageVersionId);