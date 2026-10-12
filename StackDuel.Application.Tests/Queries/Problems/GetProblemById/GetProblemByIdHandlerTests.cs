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

    public GetProblemByIdHandlerTests()
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

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemByIdQuery(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
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

        Assert.True(result.IsSuccess);
        ProblemWithSetupsDto dto = result.Value;
        Assert.Equal(problem.Id, dto.Id);
        Assert.Equal("two-sum", dto.Slug);
        Assert.Equal("Two Sum", dto.Title);
        Assert.Equal(problem.Difficulty.Tier, dto.DifficultyTier);
        Assert.Equal(problem.Question.Value, dto.Question);
        Assert.Equivalent(new[] { "arrays" }, dto.Tags, strict: true);
        Assert.Null(dto.Author);
        Assert.Empty(dto.PublicTestCases);
        Assert.Empty(dto.AvailableLanguages);
    }

    [Fact]
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

        Assert.Empty(result.Value.PublicTestCases);
    }

    [Fact]
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
        Assert.Equal(language.Id, mappedLanguage.Id);
        Assert.Equal("Python", mappedLanguage.Name);
        Assert.Equal("3.12", mappedLanguage.Versions.Single().Version);
    }
}