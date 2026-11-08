using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemSetups;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Tests.Queries.StudioProblems.GetStudioProblemSetups;

public class GetStudioProblemSetupsHandlerTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly Mock<ILanguageReadRepository> _languageReadRepository = new();
    private readonly GetStudioProblemSetupsHandler _handler;

    public GetStudioProblemSetupsHandlerTests()
    {
        _handler = new GetStudioProblemSetupsHandler(_problemReadRepository.Object, _languageReadRepository.Object);

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
            .Setup(x => x.FindByIdForAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        var query = new GetStudioProblemSetupsQuery(Guid.NewGuid(), Guid.NewGuid());
        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemWithNoSetups_ReturnsEmpty()
    {
        var problem = CreateProblem();
        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_SetupWithKnownLanguageVersion_MapsLanguageAndVersionDetails()
    {
        var problem = CreateProblem();
        var language = new Language(new LanguageName("JavaScript"), new LanguageSlug("javascript"), Guid.NewGuid());
        var versionEntry = language.AddVersion(new LanguageVersion("ES2020"), new Judge0Id(63));

        ProblemSetup setup = problem.AddSetup(versionEntry.Id, "function solve() {}", "solve", Guid.NewGuid());
        problem.SetReferenceSolution(setup.Id, "function solve() { return 1; }");

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemDetailsSetupDto dto = result.Value.Single();
        Assert.Equal(versionEntry.Id, dto.LanguageVersionId);
        Assert.Equal("ES2020", dto.LanguageVersion);
        Assert.Equal(language.Id, dto.LanguageId);
        Assert.Equal("JavaScript", dto.Language);
        Assert.Equal("function solve() {}", dto.StarterCode);
        Assert.Equal("solve", dto.FunctionName);
        Assert.Equal("function solve() { return 1; }", dto.ReferenceSolution);
        Assert.Equal("0", dto.AmountOfTestSuites);
        Assert.Equal("0", dto.AmountOfTestCases);
    }

    [Fact]
    public async Task Handle_SetupWithUnknownLanguageVersion_FallsBackToUnknownLabels()
    {
        var problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemDetailsSetupDto dto = result.Value.Single();
        Assert.Equal(Guid.Empty, dto.LanguageId);
        Assert.Equal("Unknown", dto.Language);
        Assert.Equal("?", dto.LanguageVersion);
    }

    [Fact]
    public async Task Handle_SetupWithoutFunctionNameOrReferenceSolution_DefaultsToEmptyStrings()
    {
        var problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "code", functionName: null, Guid.NewGuid());

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemDetailsSetupDto dto = result.Value.Single();
        Assert.Equal(string.Empty, dto.FunctionName);
        Assert.Equal(string.Empty, dto.ReferenceSolution);
    }

    [Fact]
    public async Task Handle_MultipleSetups_ReturnsOneDtoPerSetup()
    {
        var problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "code one", "solveOne", Guid.NewGuid());
        problem.AddSetup(Guid.NewGuid(), "code two", "solveTwo", Guid.NewGuid());

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(2, result.Value.Count());
    }
}
