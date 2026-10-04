using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemSetup : Entity
{
    internal ProblemSetup(Guid languageVersionId, string initialCode, string? functionName, Guid pipelineId)
    {
        LanguageVersionId =
            languageVersionId != Guid.Empty
                ? languageVersionId
                : throw new ArgumentException("Language version id must not be empty.", nameof(languageVersionId));

        InitialCode = !string.IsNullOrWhiteSpace(initialCode)
            ? initialCode
            : throw new ArgumentException("Initial code must not be empty.", nameof(initialCode));

        if (functionName is not null && string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name must not be blank when provided.", nameof(functionName));

        FunctionName = functionName;

        PipelineId =
            pipelineId != Guid.Empty
                ? pipelineId
                : throw new ArgumentException("Pipeline id must not be empty.", nameof(pipelineId));
    }

    internal void SetAdditionalFileBundle(Guid bundleId)
    {
        AdditionalFileBundleId =
            bundleId != Guid.Empty
                ? bundleId
                : throw new ArgumentException("Additional file bundle id must not be empty.", nameof(bundleId));
    }

    internal void ClearAdditionalFileBundle()
    {
        AdditionalFileBundleId = null;
    }

    private ProblemSetup() { }

    public IEnumerable<TestSuite> PublicTestSuites() =>
        [.. _testSuites.Where(testSuite => testSuite.Type == TestSuiteType.Sample)];

    internal void SetReferenceSolution(string code)
    {
        ReferenceSolutionCode = !string.IsNullOrWhiteSpace(code)
            ? code
            : throw new ArgumentException("Reference solution code must not be empty.", nameof(code));
    }

    public void UpdateInitialCode(string initialCode)
    {
        InitialCode = !string.IsNullOrWhiteSpace(initialCode)
            ? initialCode
            : throw new ArgumentException("Initial code must not be empty.", nameof(initialCode));
    }

    public void UpdateFunctionName(string? functionName)
    {
        if (functionName is not null && string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name must not be blank when provided.", nameof(functionName));

        FunctionName = functionName;
    }

    internal void SetGenerationSpecId(Guid specId)
    {
        GenerationSpecId =
            specId != Guid.Empty
                ? specId
                : throw new ArgumentException("Generation spec id must not be empty.", nameof(specId));
    }

    /// <summary>
    /// Sets the setup's additional starter files (e.g. helper components alongside the main
    /// solution file) — editable in the workspace like the main file, unlike
    /// <see cref="AdditionalFileBundleId"/> which is an opaque, non-editable static asset.
    /// </summary>
    public void SetAdditionalFiles(IEnumerable<ProblemSetupFile> files)
    {
        AdditionalFiles = [.. files];
    }

    public Guid LanguageVersionId { get; private set; }
    public Guid PipelineId { get; private set; }
    public string InitialCode { get; private set; } = null!;
    public string? FunctionName { get; private set; }
    public string? ReferenceSolutionCode { get; private set; }
    public Guid? GenerationSpecId { get; private set; }
    public Guid? AdditionalFileBundleId { get; private set; }
    public IReadOnlyList<ProblemSetupFile> AdditionalFiles { get; private set; } = [];

    public IReadOnlyCollection<TestSuite> TestSuites => _testSuites.AsReadOnly();

    private readonly List<TestSuite> _testSuites = [];
}