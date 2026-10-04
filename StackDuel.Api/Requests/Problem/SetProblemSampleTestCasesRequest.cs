namespace StackDuel.Api.Requests.Problem;

public sealed record SampleTestCaseInputRequestItem(string Value, string ValueType);

public sealed record SampleTestCaseRequestItem(
    string? Name,
    IReadOnlyList<SampleTestCaseInputRequestItem> Inputs,
    string ExpectedOutputValue,
    string ExpectedOutputValueType
);

public sealed record SetProblemSampleTestCasesRequest(IReadOnlyList<SampleTestCaseRequestItem> TestCases);