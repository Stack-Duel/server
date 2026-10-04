using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using GetProblemByIdHandler = StackDuel.Application.Queries.Problems.GetProblemById.GetProblemByIdHandler;
using GetProblemByIdQuery = StackDuel.Application.Queries.Problems.GetProblemById.GetProblemByIdQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetProblemById;

public class GetProblemByIdHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetProblemByIdHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetProblemByIdHandler(_problemReadRepository.Object, _languageReadRepository.Object);

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    [Test]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemByIdQuery(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_ProblemWithoutSetups_MapsCoreFieldsAndEmptyTestCases()
    {
        var problem = CreateProblem();
        problem.AddTag(new ProblemTag(new Tag("arrays")));
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemByIdQuery(problem.Id),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        ProblemWithSetupsDto dto = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(problem.Id));
            Assert.That(dto.Slug, Is.EqualTo("two-sum"));
            Assert.That(dto.Title, Is.EqualTo("Two Sum"));
            Assert.That(dto.DifficultyTier, Is.EqualTo(problem.Difficulty.Tier));
            Assert.That(dto.Question, Is.EqualTo(problem.Question.Value));
            Assert.That(dto.Tags, Is.EquivalentTo(new[] { "arrays" }));
            Assert.That(dto.Author, Is.Null);
            Assert.That(dto.PublicTestCases, Is.Empty);
            Assert.That(dto.AvailableLanguages, Is.Empty);
        });
    }

    [Test]
    public async Task Handle_ProblemWithSetupButNoSampleSuites_ReturnsEmptyPublicTestCases()
    {
        var problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemByIdQuery(problem.Id),
            CancellationToken.None
        );

        Assert.That(result.Value.PublicTestCases, Is.Empty);
    }

    [Test]
    public async Task Handle_AvailableLanguages_MapsLanguageAndVersions()
    {
        var problem = CreateProblem();
        var language = new Language(new LanguageName("Python"), new LanguageSlug("python"), Guid.NewGuid());
        var versionEntry = language.AddVersion(new LanguageVersion("3.12"), new Judge0Id(71));
        problem.AddSetup(versionEntry.Id, "code", "solve", Guid.NewGuid());

        _problemReadRepository
            .Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemByIdQuery(problem.Id),
            CancellationToken.None
        );

        var mappedLanguage = result.Value.AvailableLanguages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(mappedLanguage.Id, Is.EqualTo(language.Id));
            Assert.That(mappedLanguage.Name, Is.EqualTo("Python"));
            Assert.That(mappedLanguage.Versions.Single().Version, Is.EqualTo("3.12"));
        });
    }
}