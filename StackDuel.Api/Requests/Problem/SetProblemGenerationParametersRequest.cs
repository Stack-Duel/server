using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem;

public sealed record GenerationParameterRequestItem(
    string Name,
    string ValueType,
    double? Min,
    double? Max,
    int? LengthMin,
    int? LengthMax,
    string? Charset
);

public sealed record SetProblemGenerationParametersRequest(
    IReadOnlyList<GenerationParameterRequestItem> Parameters,
    string OutputValueType,
    [property: JsonRequired] int TargetCaseCount,
    [property: JsonRequired] int Seed
);