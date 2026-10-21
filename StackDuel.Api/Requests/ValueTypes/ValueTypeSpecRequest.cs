namespace StackDuel.Api.Requests.ValueTypes;

public sealed record ValueTypeSpecRequest(
    string Kind,
    double? Min,
    double? Max,
    int? LengthMin,
    int? LengthMax,
    string? Charset,
    ValueTypeSpecRequest? Element
);