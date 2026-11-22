using Ardalis.Result;
using Moq;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites;
using StackDuel.Domain.TestSuites.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemSampleTestCases;

public class SetProblemSampleTestCasesHandlerTests
{
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<ITestSuiteWriteRepository> _testSuiteWriteRepository = new();
    private readonly SetProblemSampleTestCasesHandler _handler;

    public SetProblemSampleTestCasesHandlerTests()
    {
        _handler = new SetProblemSampleTestCasesHandler(
            new SetProblemSampleTestCasesValidator(),
            _problemRepository.Object,
            _testSuiteWriteRepository.Object
        );
    }

    private static Problem CreateProblemWithSetup()
    {
        Problem problem = new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );
        problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        return problem;
    }

    private Problem SetUpProblem(Problem problem)
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        return problem;
    }

    private static List<SampleTestCaseDto> OneCase() =>
        [
            new SampleTestCaseDto(
                "two positives",
                [new SampleTestCaseInputDto("[1,2]", "integer_array")],
                ExpectedOutputValue: "3",
                ExpectedOutputValueType: "integer"
            ),
        ];

    private Task<Result> SetAsync(Problem problem, IReadOnlyList<SampleTestCaseDto>? testCases = null) =>
        _handler.Handle(new SetProblemSampleTestCasesCommand(problem.Id, testCases ?? OneCase()), default);

    [Fact]
    public async Task MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(
            new SetProblemSampleTestCasesCommand(Guid.NewGuid(), OneCase()),
            default
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ADeletedProblem_ReturnsNotFound()
    {
        Problem problem = CreateProblemWithSetup();
        problem.Delete();
        SetUpProblem(problem);

        Result result = await SetAsync(problem);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _testSuiteWriteRepository.Verify(
            repository =>
                repository.ReplaceSampleTestCasesAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<AuthoredTestCaseSpec>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task AProblemMidValidation_CannotHaveItsSamplesRewritten()
    {
        Problem problem = CreateProblemWithSetup();
        problem.SubmitForValidation();
        SetUpProblem(problem);

        Result result = await SetAsync(problem);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "Status");
    }

    [Fact]
    public async Task AProblemWithNoSetups_IsRejected()
    {
        Problem problem = new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );
        SetUpProblem(problem);

        Result result = await SetAsync(problem);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "TestCases");
    }

    [Fact]
    public async Task RewritingTheSamplesOfAPublishedProblem_LeavesItWithUnpublishedChanges()
    {
        Problem problem = CreateProblemWithSetup();
        problem.SubmitForValidation();
        problem.CompleteValidation();
        SetUpProblem(problem);

        Result result = await SetAsync(problem);

        Assert.True(result.IsSuccess);
        Assert.True(problem.HasUnpublishedChanges);
    }

    [Fact]
    public async Task TheSamples_AreWrittenAgainstEverySetup()
    {
        Problem problem = SetUpProblem(CreateProblemWithSetup());
        problem.AddSetup(Guid.NewGuid(), "other code", "solve", Guid.NewGuid());
        IReadOnlyList<Guid> setupIds = [.. problem.Setups.Select(setup => setup.Id)];

        Result result = await SetAsync(problem);

        Assert.True(result.IsSuccess);
        _testSuiteWriteRepository.Verify(
            repository =>
                repository.ReplaceSampleTestCasesAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.OrderBy(id => id).SequenceEqual(setupIds.OrderBy(id => id))),
                    "two-sum - Sample Cases",
                    It.Is<IReadOnlyList<AuthoredTestCaseSpec>>(specs =>
                        specs.Count == 1
                        && specs[0].Name == "two positives"
                        && specs[0].ExpectedOutputValue == "3"
                        && specs[0].Inputs.Count == 1
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}