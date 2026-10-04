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

    [SetUp]
    public void SetUp()
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

    [Test]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        var query = new GetAdminProblemDetailQuery(Guid.NewGuid());
        Result<AdminProblemDetailDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
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

        Assert.That(result.IsSuccess, Is.True);
        AdminProblemDetailDto dto = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(problem.Id));
            Assert.That(dto.Slug, Is.EqualTo("two-sum"));
            Assert.That(dto.Title, Is.EqualTo("Two Sum"));
            Assert.That(dto.DifficultyValue, Is.EqualTo(150));
            Assert.That(dto.DifficultyTier, Is.EqualTo(DifficultyTier.Beginner));
            Assert.That(dto.TimeLimitMs, Is.EqualTo(1000));
            Assert.That(dto.MemoryLimitMb, Is.EqualTo(256));
            Assert.That(dto.Status, Is.EqualTo(ProblemStatus.Draft));
            Assert.That(dto.CreatedByUsername, Is.Null);
            Assert.That(dto.Tags, Is.EquivalentTo(new[] { "arrays", "hash-map" }));
            Assert.That(dto.PoolKeys, Is.Empty);
            Assert.That(dto.Setups, Is.Empty);
        });
    }

    [Test]
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

        Assert.That(result.Value.PoolKeys, Is.EquivalentTo(new[] { "daily", "interview-prep" }));
    }

    [Test]
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
        Assert.Multiple(() =>
        {
            Assert.That(setupDto.LanguageVersionId, Is.EqualTo(versionEntry.Id));
            Assert.That(setupDto.LanguageName, Is.EqualTo("JavaScript"));
            Assert.That(setupDto.LanguageVersion, Is.EqualTo("ES2020"));
            Assert.That(setupDto.FunctionName, Is.EqualTo("solve"));
            Assert.That(setupDto.InitialCode, Is.EqualTo("function solve() {}"));
            Assert.That(setupDto.HasReferenceSolution, Is.True);
            Assert.That(setupDto.HasGenerationSpec, Is.True);
            Assert.That(setupDto.TestSuiteCount, Is.Zero);
            Assert.That(setupDto.TestCaseCount, Is.Zero);
        });
    }

    [Test]
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
        Assert.Multiple(() =>
        {
            Assert.That(setupDto.Id, Is.EqualTo(setup.Id));
            Assert.That(setupDto.LanguageName, Is.EqualTo("Unknown"));
            Assert.That(setupDto.LanguageVersion, Is.EqualTo("?"));
            Assert.That(setupDto.HasReferenceSolution, Is.False);
            Assert.That(setupDto.HasGenerationSpec, Is.False);
        });
    }
}