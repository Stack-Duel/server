using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.Tracks.Entities;
using GetAdminProblemDetailHandler = StackDuel.Application.Queries.Problems.GetAdminProblemDetail.GetAdminProblemDetailHandler;
using GetAdminProblemDetailQuery = StackDuel.Application.Queries.Problems.GetAdminProblemDetail.GetAdminProblemDetailQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetAdminProblemDetail;

public class GetAdminProblemDetailHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private Mock<IProblemPoolRepository> _problemPoolRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private GetAdminProblemDetailHandler _handler = null!;

    public GetAdminProblemDetailHandlerTests()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _problemPoolRepository = new Mock<IProblemPoolRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _handler = new GetAdminProblemDetailHandler(
            _problemReadRepository.Object,
            _languageReadRepository.Object,
            _problemPoolRepository.Object,
            _trackReadRepository.Object
        );

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _problemPoolRepository
            .Setup(x => x.GetPoolKeysForProblemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _trackReadRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Track?)null);
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

        var query = new GetAdminProblemDetailQuery(Guid.NewGuid());
        Result<AdminProblemDetailDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemFound_MapsCoreFields()
    {
        var problem = CreateProblem();
        problem.AddTag(new ProblemTag(new Tag("arrays")));
        problem.AddTag(new ProblemTag(new Tag("hash-map")));
        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        var query = new GetAdminProblemDetailQuery(problem.Id);
        Result<AdminProblemDetailDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        AdminProblemDetailDto dto = result.Value;
        Assert.Equal(problem.Id, dto.Id);
        Assert.Equal("two-sum", dto.Slug);
        Assert.Equal("Two Sum", dto.Title);
        Assert.Equal(150, dto.DifficultyValue);
        Assert.Equal(DifficultyTier.Beginner, dto.DifficultyTier);
        Assert.Equal(1000, dto.TimeLimitMs);
        Assert.Equal(256, dto.MemoryLimitMb);
        Assert.Equal(ProblemStatus.Draft, dto.Status);
        Assert.Null(dto.CreatedByUsername);
        Assert.Equivalent(new[] { "arrays", "hash-map" }, dto.Tags, strict: true);
        Assert.Empty(dto.PoolKeys);
        Assert.Empty(dto.Setups);
    }

    [Fact]
    public async Task Handle_ProblemInPools_MapsPoolKeys()
    {
        var problem = CreateProblem();
        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _problemPoolRepository
            .Setup(x => x.GetPoolKeysForProblemAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["daily", "interview-prep"]);

        Result<AdminProblemDetailDto> result = await _handler.Handle(
            new GetAdminProblemDetailQuery(problem.Id),
            CancellationToken.None
        );

        Assert.Equivalent(new[] { "daily", "interview-prep" }, result.Value.PoolKeys, strict: true);
    }

    [Fact]
    public async Task Handle_SetupWithKnownLanguageVersion_MapsLanguageNameAndVersion()
    {
        var problem = CreateProblem();
        var pipelineId = Guid.NewGuid();

        var language = new Language(new LanguageName("JavaScript"), new LanguageSlug("javascript"), Guid.NewGuid());
        var versionEntry = language.AddVersion(new LanguageVersion("ES2020"), new Judge0Id(63));

        ProblemSetup setup = problem.AddSetup(versionEntry.Id, "function solve() {}", "solve", pipelineId);
        problem.SetReferenceSolution(setup.Id, "function solve() { return 1; }");
        problem.SetGenerationSpecId(setup.Id, Guid.NewGuid());

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        Result<AdminProblemDetailDto> result = await _handler.Handle(
            new GetAdminProblemDetailQuery(problem.Id),
            CancellationToken.None
        );

        AdminProblemSetupDto setupDto = result.Value.Setups.Single();
        Assert.Equal(versionEntry.Id, setupDto.LanguageVersionId);
        Assert.Equal("JavaScript", setupDto.LanguageName);
        Assert.Equal("ES2020", setupDto.LanguageVersion);
        Assert.Equal("solve", setupDto.FunctionName);
        Assert.Equal("function solve() {}", setupDto.InitialCode);
        Assert.True(setupDto.HasReferenceSolution);
        Assert.True(setupDto.HasGenerationSpec);
        Assert.Equal(0, setupDto.TestSuiteCount);
        Assert.Equal(0, setupDto.TestCaseCount);
    }

    [Fact]
    public async Task Handle_SetupWithUnknownLanguageVersion_FallsBackToUnknownLabels()
    {
        var problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<AdminProblemDetailDto> result = await _handler.Handle(
            new GetAdminProblemDetailQuery(problem.Id),
            CancellationToken.None
        );

        AdminProblemSetupDto setupDto = result.Value.Setups.Single();
        Assert.Equal(setup.Id, setupDto.Id);
        Assert.Equal("Unknown", setupDto.LanguageName);
        Assert.Equal("?", setupDto.LanguageVersion);
        Assert.False(setupDto.HasReferenceSolution);
        Assert.False(setupDto.HasGenerationSpec);
    }
}