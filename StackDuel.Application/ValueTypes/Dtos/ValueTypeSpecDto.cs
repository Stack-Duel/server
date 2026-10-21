namespace StackDuel.Application.ValueTypes.Dtos;

public sealed record ValueTypeSpecDto(
    string Kind,
    double? Min,
    double? Max,
    int? LengthMin,
    int? LengthMax,
    string? Charset,
    ValueTypeSpecDto? Element
);