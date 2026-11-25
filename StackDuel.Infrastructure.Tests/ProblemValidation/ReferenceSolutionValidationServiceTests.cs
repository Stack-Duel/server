using Moq;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.Languages;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Infrastructure.ExecutionEngine;
using StackDuel.Infrastructure.ProblemValidation;

namespace StackDuel.Infrastructure.Tests.ProblemValidation;

public class ReferenceSolutionValidationServiceTests
{
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<ILanguageReadRepository> _languageReadRepository = new();
    private readonly Mock<ICodeTemplateStrategyResolver> _templateResolver = new();
    private readonly ReferenceSolutionValidationService _service;

    public ReferenceSolutionValidationServiceTests()
    {
        _languageReadRepository
            .Setup(repository =>
                repository.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        _service = new ReferenceSolutionValidationService(
            _problemRepository.Object,
            _languageReadRepository.Object,
            _templateResolver.Object,
            new Judge0ReferenceSolutionRunner(Mock.Of<IExecutionEngineStrategy>())
        );
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("minimum-coins-for-change"),
            new Title("Minimum Coins for Change"),
            new Question(new string('q', 60)),
            new Difficulty(500),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private Problem SetUpProblem(Problem problem)
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        return problem;
    }

    [Fact]
    public async Task RetiredSetupsAreNotValidated()
    {
        Problem problem = CreateProblem();
        Guid activeVersionId = Guid.NewGuid();
        Guid retiredVersionId = Guid.NewGuid();
        problem.AddSetup(activeVersionId, "# python", "min_coins", Guid.NewGuid());
        ProblemSetup retired = problem.AddSetup(retiredVersionId, "// java", "minCoins", Guid.NewGuid());
        problem.RetireSetup(retired.Id);
        SetUpProblem(problem);

        ReferenceSolutionValidationResult result = await _service.ValidateAsync(problem.Id, CancellationToken.None);

        SetupValidationOutcome outcome = Assert.Single(result.Outcomes);
        Assert.Equal(activeVersionId.ToString(), outcome.LanguageLabel);
        Assert.DoesNotContain(result.Outcomes, o => o.ProblemSetupId == retired.Id);
    }

    [Fact]
    public async Task OnlyActiveSetupLanguagesAreLookedUp()
    {
        Problem problem = CreateProblem();
        Guid activeVersionId = Guid.NewGuid();
        Guid retiredVersionId = Guid.NewGuid();
        problem.AddSetup(activeVersionId, "# python", "min_coins", Guid.NewGuid());
        ProblemSetup retired = problem.AddSetup(retiredVersionId, "// java", "minCoins", Guid.NewGuid());
        problem.RetireSetup(retired.Id);
        SetUpProblem(problem);

        await _service.ValidateAsync(problem.Id, CancellationToken.None);

        _languageReadRepository.Verify(
            repository =>
                repository.FindLanguagesByVersionId(
                    It.Is<IEnumerable<Guid>>(ids => ids.Single() == activeVersionId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}