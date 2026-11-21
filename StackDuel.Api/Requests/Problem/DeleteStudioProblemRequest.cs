using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem;

public sealed record DeleteStudioProblemRequest([property: JsonRequired] string ConfirmationTitle);