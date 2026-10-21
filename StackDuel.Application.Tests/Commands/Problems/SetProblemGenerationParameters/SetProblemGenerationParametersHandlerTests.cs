using Ardalis.Result;
using Moq;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using SetProblemGenerationParametersCommand = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersCommand;
using SetProblemGenerationParametersHandler = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersHandler;
using SetProblemGenerationParametersValidator = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemGenerationParameters;

public class SetProblemGenerationParametersHandlerTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private Mock<IProblemRepository> _problemRepository = null!;
    private Mock<IValueTypeDefinitionRepository> _valueTypeDefinitionRepository = null!;
    private SetProblemGenerationParametersHandler _handler = null!;

    public SetProblemGenerationParametersHandlerTests()
    {
        _problemRepository = new Mock<IProblemRepository>();
        _valueTypeDefinitionRepository = new Mock<IValueTypeDefinitionRepository>();

        GivenCatalog(
            Definition(WellKnownValueTypes.Integer, new ValueTypeSpec(ValueTypeKind.Integer)),
            Definition(WellKnownValueTypes.String, new ValueTypeSpec(ValueTypeKind.String)),
            Definition(WellKnownValueTypes.Boolean, new ValueTypeSpec(ValueTypeKind.Boolean)),
            Definition(
                WellKnownValueTypes.IntegerArray,
                new ValueTypeSpec(ValueTypeKind.Array, element: new ValueTypeSpec(ValueTypeKind.Integer))
            )
        );

        _handler = new SetProblemGenerationParametersHandler(
            new SetProblemGenerationParametersValidator(),
            _problemRepository.Object,
            _valueTypeDefinitionRepository.Object
        );
    }

    private static ValueTypeDefinition Definition(string key, ValueTypeSpec spec) =>
        new(new ValueTypeKey(key), key, null, spec, isBuiltIn: true);

    private void GivenCatalog(params ValueTypeDefinition[] definitions) =>
        _valueTypeDefinitionRepository
            .Setup(x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(definitions);

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(ValidQuestion),
            new Difficulty(100),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    private Problem GivenProblem()
    {
        Problem problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        return problem;
    }

    private static GenerationParameterSpec Parameter(
        string valueType,
        double? min = null,
        double? max = null,
        int? lengthMin = null,
        int? lengthMax = null,
        string? charset = null
    ) => new("nums", valueType, min, max, lengthMin, lengthMax, charset);

    private static SetProblemGenerationParametersCommand Command(
        Guid problemId,
        string outputValueType = WellKnownValueTypes.Integer,
        params GenerationParameterSpec[] parameters
    ) => new(problemId, parameters, outputValueType, TargetCaseCount: 20, Seed: 42);

    private static string MessagesOf(Result result) =>
        string.Join(" | ", result.ValidationErrors.Select(error => error.ErrorMessage));

    [Fact]
    public async Task Handle_ValidParameters_PersistsThem()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.Integer, min: 0, max: 10)),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.NotNull(problem.GenerationSpec);
        _problemRepository.Verify(x => x.UpdateAsync(problem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownValueType_IsRejectedWithTheAvailableOnes()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter("intger")),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("intger", MessagesOf(result));
        Assert.Contains(WellKnownValueTypes.Integer, MessagesOf(result));
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownOutputValueType_IsRejected()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, outputValueType: "nope", parameters: Parameter(WellKnownValueTypes.Integer)),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("nope", MessagesOf(result));
    }

    [Fact]
    public async Task Handle_StringLengthOnANumber_IsRejected()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.Integer, lengthMin: 1, lengthMax: 5)),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("lengthMin", MessagesOf(result));
        Assert.Contains("lengthMax", MessagesOf(result));
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CharsetOnANumber_IsRejectedAndSaysWhatIsAccepted()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.Integer, charset: "abc")),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("charset", MessagesOf(result));
        Assert.Contains("It accepts min, max.", MessagesOf(result));
    }

    [Fact]
    public async Task Handle_MinMaxOnAString_IsRejected()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.String, min: 1, max: 9)),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("min", MessagesOf(result));
    }

    [Fact]
    public async Task Handle_AnyConstraintOnABoolean_IsRejected()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.Boolean, min: 1)),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("It accepts no constraints.", MessagesOf(result));
    }

    [Fact]
    public async Task Handle_LengthAndElementRangeOnANumberArray_IsAccepted()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(
                problem.Id,
                parameters: Parameter(WellKnownValueTypes.IntegerArray, min: 0, max: 9, lengthMin: 2, lengthMax: 6)
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_CharsetOnANumberArray_IsRejected()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.IntegerArray, charset: "ab")),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("charset", MessagesOf(result));
    }

    [Fact]
    public async Task Handle_EveryOffendingParameter_IsReportedNotJustTheFirst()
    {
        Problem problem = GivenProblem();

        Result result = await _handler.Handle(
            Command(
                problem.Id,
                parameters:
                [
                    Parameter(WellKnownValueTypes.Integer, charset: "ab"),
                    Parameter(WellKnownValueTypes.String, min: 1),
                ]
            ),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(2, result.ValidationErrors.Count());
    }

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(
            Command(Guid.NewGuid(), parameters: Parameter(WellKnownValueTypes.Integer)),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemPendingValidation_IsRejectedBeforeTouchingTheCatalog()
    {
        Problem problem = GivenProblem();
        problem.SubmitForValidation();

        Result result = await _handler.Handle(
            Command(problem.Id, parameters: Parameter(WellKnownValueTypes.Integer)),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _valueTypeDefinitionRepository.Verify(
            x => x.GetAllOrderedAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}