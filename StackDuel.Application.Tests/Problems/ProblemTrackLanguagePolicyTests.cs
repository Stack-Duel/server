using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Tests.Problems;

public class ProblemTrackLanguagePolicyTests
{
    private readonly Mock<ILanguageReadRepository> _languageReadRepository = new();
    private readonly IProblemTrackLanguagePolicy _policy;

    public ProblemTrackLanguagePolicyTests()
    {
        _policy = new ProblemTrackLanguagePolicy(_languageReadRepository.Object);
    }

    private static Problem CreateProblem(Guid? trackId = null)
    {
        Problem problem = new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

        if (trackId.HasValue)
            problem.SetTrack(trackId.Value);

        return problem;
    }

    private static Language CreateLanguage() =>
        new(new LanguageName("JavaScript"), new LanguageSlug("javascript"), Guid.NewGuid());

    private void SetUpTrackLanguages(Guid trackId, Language language, params Guid[] versionIds)
    {
        _languageReadRepository
            .Setup(repository =>
                repository.GetActiveLanguagesByTrackIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => ids.Single() == trackId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([language]);

        _languageReadRepository
            .Setup(repository =>
                repository.GetLanguageVersionIdsByLanguageIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(versionIds);
    }

    [Fact]
    public async Task AProblemWithNoTrack_AllowsNothing()
    {
        Problem problem = CreateProblem();

        bool allowed = await _policy.AllowsLanguageVersionAsync(problem, Guid.NewGuid(), CancellationToken.None);

        Assert.False(allowed);
        _languageReadRepository.Verify(
            repository =>
                repository.GetActiveLanguagesByTrackIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task AVersionOnTheProblemsTrack_IsAllowed()
    {
        Guid trackId = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        Problem problem = CreateProblem(trackId);
        SetUpTrackLanguages(trackId, CreateLanguage(), versionId);

        bool allowed = await _policy.AllowsLanguageVersionAsync(problem, versionId, CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    public async Task AVersionOutsideTheProblemsTrack_IsRejected()
    {
        Guid trackId = Guid.NewGuid();
        Problem problem = CreateProblem(trackId);
        SetUpTrackLanguages(trackId, CreateLanguage(), Guid.NewGuid());

        bool allowed = await _policy.AllowsLanguageVersionAsync(problem, Guid.NewGuid(), CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    public async Task ATrackWithNoActiveLanguages_AllowsNothing()
    {
        Guid trackId = Guid.NewGuid();
        Problem problem = CreateProblem(trackId);

        _languageReadRepository
            .Setup(repository =>
                repository.GetActiveLanguagesByTrackIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
        _languageReadRepository
            .Setup(repository =>
                repository.GetLanguageVersionIdsByLanguageIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        bool allowed = await _policy.AllowsLanguageVersionAsync(problem, Guid.NewGuid(), CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    public async Task TheTracksLanguageIds_AreWhatTheVersionLookupIsAskedFor()
    {
        Guid trackId = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        Language language = CreateLanguage();
        Problem problem = CreateProblem(trackId);
        SetUpTrackLanguages(trackId, language, versionId);

        await _policy.AllowsLanguageVersionAsync(problem, versionId, CancellationToken.None);

        _languageReadRepository.Verify(
            repository =>
                repository.GetLanguageVersionIdsByLanguageIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => ids.Single() == language.Id),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}