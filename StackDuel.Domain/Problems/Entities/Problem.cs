using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites.Enums;
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

    public void Delete()
    {
        DeletedAt ??= DateTime.UtcNow;
    }

    public bool IsDeleted => DeletedAt.HasValue;

    public void Publish()
    {
        Status = ProblemStatus.Published;
        FreezeVersion();
    }

    public void SubmitForValidation()
    {
        ValidationFailureReason = null;

        if (IsPublished)
        {
            IsRevalidating = true;
            return;
        }

        Status = ProblemStatus.Pending;
    }

    public bool IsPublished => PublishedVersionId.HasValue && Status == ProblemStatus.Published;

    public bool IsEditLocked => Status == ProblemStatus.Pending || IsRevalidating;

    public bool CanSubmitForValidation =>
        !IsEditLocked
        && (Status is ProblemStatus.Draft or ProblemStatus.Failed || (IsPublished && HasUnpublishedChanges));

    public void FailValidation(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason must not be empty.", nameof(reason));

        ValidationFailureReason =
            reason.Length > MaxValidationFailureReasonLength ? reason[..MaxValidationFailureReasonLength] : reason;

        if (IsRevalidating)
        {
            IsRevalidating = false;
            return;
        }

        Status = ProblemStatus.Failed;
    }

    public const int MaxValidationFailureReasonLength = 4000;

    public void CancelValidation()
    {
        if (IsRevalidating)
        {
            IsRevalidating = false;
            ValidationFailureReason = null;
            return;
        }

        if (Status != ProblemStatus.Pending)
            return;

        Status = ProblemStatus.Draft;
        ValidationFailureReason = null;
    }

    public void CompleteValidation()
    {
        Status = ProblemStatus.Published;
        ValidationFailureReason = null;
        IsRevalidating = false;
        FreezeVersion();
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
        MarkContentChanged();
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
        MarkContentChanged();
    }

    public void UpdateSlug(Slug slug)
    {
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
    }

    public ProblemSetup AddSetup(Guid languageVersionId, string initialCode, string? functionName, Guid pipelineId)
    {
        var setup = new ProblemSetup(languageVersionId, initialCode, functionName, pipelineId);
        _setups.Add(setup);
        MarkContentChanged();
        return setup;
    }

    public void SetReferenceSolution(Guid setupId, string code)
    {
        ProblemSetup setup = _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.SetReferenceSolution(code);
        MarkContentChanged();
    }

    public void SetGenerationSpecId(Guid setupId, Guid specId)
    {
        ProblemSetup setup = _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.SetGenerationSpecId(specId);
    }

    public void SetAdditionalFileBundle(Guid setupId, Guid bundleId)
    {
        ProblemSetup setup = _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.SetAdditionalFileBundle(bundleId);
    }

    public void UpdateSetupInitialCode(Guid setupId, string initialCode)
    {
        ProblemSetup setup = _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.UpdateInitialCode(initialCode);
        MarkContentChanged();
    }

    public void UpdateSetupFunctionName(Guid setupId, string? functionName)
    {
        ProblemSetup setup = _setups.FirstOrDefault(s => s.Id == setupId)
            ?? throw new InvalidOperationException($"Setup {setupId} not found on problem.");
        setup.UpdateFunctionName(functionName);
        MarkContentChanged();
    }

    public void AddTag(ProblemTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (_tags.Contains(tag))
            return;

        _tags.Add(tag);
        MarkContentChanged();
    }

    public void RemoveTag(ProblemTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (_tags.Remove(tag))
            MarkContentChanged();
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

    public void MarkSampleTestCasesChanged() => MarkContentChanged();

    private void MarkContentChanged()
    {
        if (PublishedVersionId.HasValue)
            HasUnpublishedChanges = true;
    }

    private void FreezeVersion()
    {
        HasUnpublishedChanges = false;

        PublishedVersion?.Supersede();

        ProblemVersion version = new(
            Id,
            NextVersionNumber,
            Title,
            Question,
            Difficulty,
            TimeLimit,
            MemoryLimit,
            _setups.Select(setup => new ProblemVersionSetupSpec(
                setup.Id,
                setup.LanguageVersionId,
                setup.InitialCode,
                setup.FunctionName
            ))
        );

        _versions.Add(version);
        PublishedVersionId = version.Id;
    }

    public ProblemVersion? PublishedVersion =>
        PublishedVersionId.HasValue ? _versions.FirstOrDefault(version => version.Id == PublishedVersionId) : null;

    public ProblemVersion? FindVersion(Guid versionId) => _versions.FirstOrDefault(version => version.Id == versionId);

    private int NextVersionNumber =>
        _versions.Count == 0 ? ProblemVersion.FirstVersionNumber : _versions.Max(version => version.VersionNumber) + 1;

    public IReadOnlyList<int> SampleTestCaseInputCounts() =>
        [
            .. _setups
                .SelectMany(setup => setup.TestSuites)
                .Where(testSuite => testSuite.Type == TestSuiteType.Sample)
                .SelectMany(testSuite => testSuite.TestCases)
                .Where(testCase => testCase.Source == TestCaseSource.Authored && testCase.RetiredAt is null)
                .Select(testCase => testCase.Inputs.Count)
                .Distinct()
                .Order(),
        ];

    public IEnumerable<ProblemSetup> SetupsMissingReferenceSolution() =>
        _setups.Where(setup => string.IsNullOrWhiteSpace(setup.ReferenceSolutionCode));

    public ProblemPublishReadiness EvaluatePublishReadiness()
    {
        int sampleTestCaseCount = _setups
            .SelectMany(setup => setup.TestSuites)
            .Where(testSuite => testSuite.Type == TestSuiteType.Sample)
            .SelectMany(testSuite => testSuite.TestCases)
            .Count(testCase => testCase.Source == TestCaseSource.Authored && testCase.RetiredAt is null);

        return new ProblemPublishReadiness(
            _setups.Count,
            SetupsMissingReferenceSolution().Count(),
            sampleTestCaseCount,
            GenerationSpec is not null
        );
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

    public const int MaxOpenProblemsPerCreator = 10;

    public const int MinTagCount = 1;
    public const int MaxTagCount = 5;

    public static readonly IReadOnlySet<ProblemStatus> ResolveSubmittedStatuses = new HashSet<ProblemStatus>
    {
        ProblemStatus.Published,
        ProblemStatus.Archived,
    };

    public DateTime? DeletedAt { get; private set; }

    public Guid? PublishedVersionId { get; private set; }

    public bool HasUnpublishedChanges { get; private set; }

    public bool IsRevalidating { get; private set; }

    public IReadOnlyCollection<ProblemVersion> Versions => _versions.AsReadOnly();

    public IReadOnlyCollection<ProblemSetup> Setups => _setups.AsReadOnly();
    public IReadOnlyCollection<ProblemTag> Tags => _tags.AsReadOnly();

    private readonly List<ProblemHistory> _history = [];
    private readonly List<ProblemSetup> _setups = [];
    private readonly List<ProblemVersion> _versions = [];
    private readonly List<ProblemTag> _tags = [];
}