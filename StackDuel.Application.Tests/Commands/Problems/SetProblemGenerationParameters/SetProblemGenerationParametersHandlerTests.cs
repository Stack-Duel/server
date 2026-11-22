using Ardalis.Result;
using Moq;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using SetProblemGenerationParametersCommand = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersCommand;
using SetProblemGenerationParametersHandler = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersHandler;
using SetProblemGenerationParametersValidator = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemGenerationParameters;

public class SetProblemGenerationParametersHandlerTests
{
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly SetProblemGenerationParametersHandler _handler;

    public SetProblemGenerationParametersHandlerTests()
    {
        _handler = new SetProblemGenerationParametersHandler(
            new SetProblemGenerationParametersValidator(),
            _problemRepository.Object
        );
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

    private Problem SetUpProblem(Problem problem)
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        return problem;
    }

    private Task<Result> SetAsync(Guid problemId) =>
        _handler.Handle(
            new SetProblemGenerationParametersCommand(
                problemId,
                [new GenerationParameterSpec("numbers", GenerationValueTypes.IntegerArray, -5, 5, 1, 20, null)],
                GenerationValueTypes.Integer,
                TargetCaseCount: 40,
                Seed: 7
            ),
            default
        );

    [Fact]
    public async Task MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await SetAsync(Guid.NewGuid());

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ADeletedProblem_ReturnsNotFound()
    {
        Problem problem = CreateProblem();
        problem.Delete();
        SetUpProblem(problem);

        Result result = await SetAsync(problem.Id);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Null(problem.GenerationSpec);
    }

    [Fact]
    public async Task AProblemMidValidation_CannotHaveItsParametersChanged()
    {
        Problem problem = CreateProblem();
        problem.SubmitForValidation();
        SetUpProblem(problem);

        Result result = await SetAsync(problem.Id);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "Status");
        Assert.Null(problem.GenerationSpec);
    }

    [Fact]
    public async Task ADraft_StoresTheParametersAndIsPersisted()
    {
        Problem problem = SetUpProblem(CreateProblem());

        Result result = await SetAsync(problem.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(problem.GenerationSpec);
        Assert.Equal(40, problem.GenerationSpec.TargetCaseCount);
        Assert.Equal(7, problem.GenerationSpec.Seed);
        Assert.Equal("numbers", Assert.Single(problem.GenerationSpec.Parameters).Name);
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}