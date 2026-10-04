using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.TestCaseGeneration;

public sealed record GenerateTestCasesRequest(
    Guid ProblemSetupId,
    string ReferenceSolutionCode,
    IReadOnlyList<GenerationParameterSpec> Parameters,
    string OutputValueType,
    int TargetCaseCount,
    int Seed
);

/// <summary>
/// <paramref name="Requested"/> is the target number of successful cases (not attempts —
/// generation keeps trying new random inputs, up to a safety cap, until this many succeed
/// or it gives up). <paramref name="Skipped"/> is how many attempts failed along the way.
/// </summary>
public sealed record GenerateTestCasesResult(int Requested, int Generated, int Skipped, IReadOnlyList<string> Warnings)
{
    public bool Succeeded => Generated >= Requested;
}

/// <summary>
/// Generates a problem's random hidden test case pool offline (triggered by whoever authors
/// the problem — the Seeder today), verified against a trusted reference solution run through
/// the same sandboxed Judge0 pipeline used for real submissions. Does not touch the live
/// grading path.
/// </summary>
public interface ITestCaseGenerationService
{
    Task<GenerateTestCasesResult> GenerateAsync(
        GenerateTestCasesRequest request,
        CancellationToken cancellationToken = default
    );
}