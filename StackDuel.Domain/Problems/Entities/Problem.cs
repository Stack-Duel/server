using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.Users.Entities;

namespace StackDuel.Domain.Problems.Entities;

public sealed class Problem : AggregateRoot
{
    public Problem(
        Slug slug,
        Title title,
        Question question,
        Difficulty difficulty,
        TimeLimit timeLimit,
        MemoryLimit memoryLimit
    )
    {
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Question = question ?? throw new ArgumentNullException(nameof(question));
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
        MemoryLimit = memoryLimit ?? throw new ArgumentNullException(nameof(memoryLimit));
        Status = ProblemStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = ProblemStatus.Archived;
    }

    public void Publish()
    {
        Status = ProblemStatus.Published;
    }

    public void SubmitForValidation()
    {
        Status = ProblemStatus.Pending;
        ValidationFailureReason = null;
    }

    public void FailValidation(string reason)
    {
        ValidationFailureReason = !string.IsNullOrWhiteSpace(reason)
            ? reason
            : throw new ArgumentException("Reason must not be empty.", nameof(reason));
        Status = ProblemStatus.Failed;
    }

    public void CompleteValidation()
    {
        Status = ProblemStatus.Published;
        ValidationFailureReason = null;
    }

    public void SetTrack(Guid trackId)
    {
        TrackId =
            trackId != Guid.Empty
                ? trackId
                : throw new ArgumentException("Track id must not be empty.", nameof(trackId));
    }

    public void SetGenerationParameters(
        IReadOnlyList<GenerationParameterSpec> parameters,
        string outputValueType,
        int targetCaseCount,
        int seed
    )
    {
        GenerationSpec = new ProblemGenerationSpec(parameters, outputValueType, targetCaseCount, seed);
    }

    public void UpdateContent(
        Title title,
        Question question,
        Difficulty difficulty,
        TimeLimit timeLimit,
        MemoryLimit memoryLimit
    )
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Question = question ?? throw new ArgumentNullException(nameof(question));
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
        MemoryLimit = memoryLimit ?? throw new ArgumentNullException(nameof(memoryLimit));

        _history.Add(new ProblemHistory(Title, Question, Difficulty, TimeLimit, MemoryLimit));
    }

    public void UpdateSlug(Slug slug)
    {
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
    }

    public ProblemSetup AddSetup(Guid languageVersionId, string initialCode, string? functionName, Guid pipelineId)
    {
        var setup = new ProblemSetup(languageVersionId, initialCode, functionName, pipelineId);
        _setups.Add(setup);
        return setup;
    }

    public void SetReferenceSolution(Guid setupId, string code)
    {
        ProblemSetup setup =
            _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.SetReferenceSolution(code);
    }

    public void SetGenerationSpecId(Guid setupId, Guid specId)
    {
        ProblemSetup setup =
            _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.SetGenerationSpecId(specId);
    }

    public void SetAdditionalFileBundle(Guid setupId, Guid bundleId)
    {
        ProblemSetup setup =
            _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.SetAdditionalFileBundle(bundleId);
    }

    public void UpdateSetupInitialCode(Guid setupId, string initialCode)
    {
        ProblemSetup setup =
            _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.UpdateInitialCode(initialCode);
    }

    public void UpdateSetupFunctionName(Guid setupId, string? functionName)
    {
        ProblemSetup setup =
            _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.UpdateFunctionName(functionName);
    }

    public void AddTag(ProblemTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (!_tags.Contains(tag))
            _tags.Add(tag);
    }

    public void RemoveTag(ProblemTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        _tags.Remove(tag);
    }

    public void SetCreatedBy(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        CreatedById = userId;
    }

    private Problem()
    {
        Slug = null!;
        Title = null!;
        Question = null!;
        Difficulty = null!;
        TimeLimit = null!;
        MemoryLimit = null!;
    }

    public IEnumerable<Guid> AvailableLanguageVersionIds() => _setups.Select(setup => setup.LanguageVersionId);

    public ProblemSetup? FindSetupByLanguageVersionId(Guid languageVersionId) =>
        _setups.SingleOrDefault(setup => setup.LanguageVersionId == languageVersionId);

    public Slug Slug { get; private set; }
    public Title Title { get; private set; }
    public Question Question { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public TimeLimit TimeLimit { get; private set; }
    public MemoryLimit MemoryLimit { get; private set; }
    public ProblemStatus Status { get; private set; }
    public string? ValidationFailureReason { get; private set; }
    public Guid? TrackId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedById { get; private set; }
    public User? CreatedBy { get; private set; }

    public ProblemGenerationSpec? GenerationSpec { get; private set; }

    public IReadOnlyCollection<ProblemHistory> History => _history.AsReadOnly();
    public IReadOnlyCollection<ProblemSetup> Setups => _setups.AsReadOnly();
    public IReadOnlyCollection<ProblemTag> Tags => _tags.AsReadOnly();

    private readonly List<ProblemHistory> _history = [];
    private readonly List<ProblemSetup> _setups = [];
    private readonly List<ProblemTag> _tags = [];
}