using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Entities;

public sealed class LanguageVersionEntry : Entity
{
    internal LanguageVersionEntry(LanguageVersion version, Judge0Id judge0Id)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Judge0Id = judge0Id ?? throw new ArgumentNullException(nameof(judge0Id));
        Status = LanguageVersionStatus.Active;
        ExecutionMode = TestExecutionMode.PerCase;
    }

    public void Activate()
    {
        Status = LanguageVersionStatus.Active;
    }

    public void Deprecate()
    {
        Status = LanguageVersionStatus.Deprecated;
    }

    /// <summary>
    /// Opts this specific language version into running test cases through its native test
    /// framework in one batched Judge0 submission instead of one submission per case. The
    /// Judge0 image behind <see cref="Judge0Id"/> must actually have that test framework
    /// installed — this is a per-version choice, not a per-language one, so a plain
    /// "JavaScript" entry can keep running PerCase while a "JavaScript (Vitest)" entry
    /// pointing at a different Judge0 image opts into batching.
    /// </summary>
    public void SetExecutionMode(TestExecutionMode mode)
    {
        ExecutionMode = mode;
    }

    private LanguageVersionEntry() { }

    public bool IsActive => Status == LanguageVersionStatus.Active;
    public Judge0Id Judge0Id { get; private set; } = null!;
    public LanguageVersionStatus Status { get; private set; }
    public LanguageVersion Version { get; private set; } = null!;
    public TestExecutionMode ExecutionMode { get; private set; }
}