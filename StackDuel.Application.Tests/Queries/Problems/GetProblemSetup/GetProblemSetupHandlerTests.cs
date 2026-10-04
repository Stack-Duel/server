using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using Ardalis.Result;
using Moq;
using GetProblemSetupHandler = StackDuel.Application.Queries.Problems.GetProblemSetup.GetProblemSetupHandler;
using GetProblemSetupQuery = StackDuel.Application.Queries.Problems.GetProblemSetup.GetProblemSetupQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetProblemSetup;

public class GetProblemSetupHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private GetProblemSetupHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _handler = new GetProblemSetupHandler(_problemReadRepository.Object);
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
            .Setup(x => x.FindBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result<ProblemSetupDto> result = await _handler.Handle(
            new GetProblemSetupQuery("missing", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_LanguageVersionNotOnProblem_ReturnsNotFound()
    {
        var problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        _problemReadRepository
            .Setup(x => x.FindBySlugAsync("two-sum", It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<ProblemSetupDto> result = await _handler.Handle(
            new GetProblemSetupQuery("two-sum", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_MatchingSetupFound_ReturnsSetupWithEmptyTestCases()
    {
        var problem = CreateProblem();
        var languageVersionId = Guid.NewGuid();
        ProblemSetup setup = problem.AddSetup(languageVersionId, "def solve(): pass", "solve", Guid.NewGuid());
        _problemReadRepository
            .Setup(x => x.FindBySlugAsync("two-sum", It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<ProblemSetupDto> result = await _handler.Handle(
            new GetProblemSetupQuery("two-sum", languageVersionId),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Id, Is.EqualTo(setup.Id));
            Assert.That(result.Value.InitialCode, Is.EqualTo("def solve(): pass"));
            Assert.That(result.Value.TestCases, Is.Empty);
        });
    }
}