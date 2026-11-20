using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemVersion : Entity
{
    internal ProblemVersion(
        Guid problemId,
        int versionNumber,
        Title title,
        Question question,
        Difficulty difficulty,
        TimeLimit timeLimit,
        MemoryLimit memoryLimit,
        IEnumerable<ProblemVersionSetupSpec> setups
    )
    {
        ProblemId =
            problemId != Guid.Empty
                ? problemId
                : throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        VersionNumber =
            versionNumber >= FirstVersionNumber
                ? versionNumber
                : throw new ArgumentOutOfRangeException(
                    nameof(versionNumber),
                    $"Version number must be at least {FirstVersionNumber}."
                );

        Title = title ?? throw new ArgumentNullException(nameof(title));
        Question = question ?? throw new ArgumentNullException(nameof(question));
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
        MemoryLimit = memoryLimit ?? throw new ArgumentNullException(nameof(memoryLimit));

        ArgumentNullException.ThrowIfNull(setups);

        _setups = [.. setups.DistinctBy(setup => setup.ProblemSetupId).Select(setup => new ProblemVersionSetup(setup))];

        State = ProblemVersionState.Published;
        PublishedAt = DateTime.UtcNow;
    }

    private ProblemVersion()
    {
        Title = null!;
        Question = null!;
        Difficulty = null!;
        TimeLimit = null!;
        MemoryLimit = null!;
    }

    public const int FirstVersionNumber = 1;

    internal void Supersede()
    {
        if (State == ProblemVersionState.Superseded)
            return;

        State = ProblemVersionState.Superseded;
        SupersededAt = DateTime.UtcNow;
    }

    public bool ReferencesSetup(Guid problemSetupId) => _setups.Any(setup => setup.ProblemSetupId == problemSetupId);

    public IReadOnlyList<Guid> ProblemSetupIds => [.. _setups.Select(setup => setup.ProblemSetupId)];

    public ProblemVersionSetup? FindSetup(Guid problemSetupId) =>
        _setups.FirstOrDefault(setup => setup.ProblemSetupId == problemSetupId);

    public ProblemVersionSetup? FindSetupByLanguageVersionId(Guid languageVersionId) =>
        _setups.FirstOrDefault(setup => setup.LanguageVersionId == languageVersionId);

    public Guid ProblemId { get; private set; }
    public int VersionNumber { get; private set; }
    public ProblemVersionState State { get; private set; }
    public Title Title { get; private set; }
    public Question Question { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public TimeLimit TimeLimit { get; private set; }
    public MemoryLimit MemoryLimit { get; private set; }
    public DateTime PublishedAt { get; private set; }
    public DateTime? SupersededAt { get; private set; }

    public IReadOnlyCollection<ProblemVersionSetup> Setups => _setups.AsReadOnly();

    private readonly List<ProblemVersionSetup> _setups = [];
}
