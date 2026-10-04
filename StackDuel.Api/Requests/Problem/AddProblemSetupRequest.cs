using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem;

public sealed record AddProblemSetupRequest([property: JsonRequired] Guid LanguageVersionId);