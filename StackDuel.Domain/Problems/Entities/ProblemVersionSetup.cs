using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed record ProblemVersionSetupSpec(
    Guid ProblemSetupId,
    Guid LanguageVersionId,
    string InitialCode,
    string? FunctionName
);

public sealed class ProblemVersionSetup : Entity
{
    internal ProblemVersionSetup(ProblemVersionSetupSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        ProblemSetupId =
            spec.ProblemSetupId != Guid.Empty
                ? spec.ProblemSetupId
                : throw new ArgumentException("Problem setup id must not be empty.", nameof(spec));

        LanguageVersionId =
            spec.LanguageVersionId != Guid.Empty
                ? spec.LanguageVersionId
                : throw new ArgumentException("Language version id must not be empty.", nameof(spec));

        InitialCode = !string.IsNullOrWhiteSpace(spec.InitialCode)
            ? spec.InitialCode
            : throw new ArgumentException("Initial code must not be empty.", nameof(spec));

        FunctionName = spec.FunctionName;
    }

    private ProblemVersionSetup() { }

    public Guid ProblemSetupId { get; private set; }
    public Guid LanguageVersionId { get; private set; }
    public string InitialCode { get; private set; } = null!;
    public string? FunctionName { get; private set; }
}