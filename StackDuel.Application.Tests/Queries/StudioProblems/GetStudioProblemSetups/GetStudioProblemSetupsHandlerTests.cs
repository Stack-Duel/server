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
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using System.Reflection;

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

    private static TestSuite AttachSuite(ProblemSetup setup, TestSuiteType type)
    {
        TestSuite suite = new($"{type} suite", type);
        FieldInfo testSuitesField = typeof(ProblemSetup).GetField(
            "_testSuites",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        ((List<TestSuite>)testSuitesField.GetValue(setup)!).Add(suite);
        return suite;
    }

    private static TestCase AddAuthoredCase(TestSuite suite, string name)
    {
        TestCase testCase = suite.AddTestCase(name);
        testCase.AddInput("[1,2]", "integer_array");
        testCase.AddExpectedOutput("3", "integer");
        return testCase;
    }

    [Fact]
    public async Task Handle_AuthoredSampleCases_AreProjectedWithInputsAndExpectedOutput()
    {
        var problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        TestSuite sampleSuite = AttachSuite(setup, TestSuiteType.Sample);
        TestCase authored = AddAuthoredCase(sampleSuite, "two positives");

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemDetailsSetupDto dto = result.Value.Single();
        StudioProblemSampleTestCaseDto sample = Assert.Single(dto.SampleTestCases);
        Assert.Equal(authored.Id, sample.Id);
        Assert.Equal("two positives", sample.Name);
        Assert.Equal("3", sample.ExpectedOutputValue);
        Assert.Equal("integer", sample.ExpectedOutputValueType);
        StudioProblemSampleTestCaseInputDto input = Assert.Single(sample.Inputs);
        Assert.Equal("[1,2]", input.Value);
        Assert.Equal("integer_array", input.ValueType);
    }

    [Fact]
    public async Task Handle_HiddenSuiteCases_AreNotProjectedAsSamples()
    {
        var problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        AddAuthoredCase(AttachSuite(setup, TestSuiteType.Hidden), "hidden one");

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Empty(result.Value.Single().SampleTestCases);
    }

    [Fact]
    public async Task Handle_RetiredSampleCases_AreLeftOut()
    {
        var problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        TestSuite sampleSuite = AttachSuite(setup, TestSuiteType.Sample);
        AddAuthoredCase(sampleSuite, "kept");
        AddAuthoredCase(sampleSuite, "retired").Retire();

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemSampleTestCaseDto sample = Assert.Single(result.Value.Single().SampleTestCases);
        Assert.Equal("kept", sample.Name);
    }

    [Fact]
    public async Task Handle_ASampleCaseWithNoExpectedOutput_FallsBackToEmptyStrings()
    {
        var problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        TestSuite sampleSuite = AttachSuite(setup, TestSuiteType.Sample);
        TestCase bare = sampleSuite.AddTestCase("no output");
        bare.AddInput("[]", "integer_array");

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemSampleTestCaseDto sample = Assert.Single(result.Value.Single().SampleTestCases);
        Assert.Equal(string.Empty, sample.ExpectedOutputValue);
        Assert.Equal(string.Empty, sample.ExpectedOutputValueType);
    }

    [Fact]
    public async Task Handle_TestSuiteAndCaseCounts_CountEverySuiteNotJustSamples()
    {
        var problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "code", "solve", Guid.NewGuid());
        TestSuite sampleSuite = AttachSuite(setup, TestSuiteType.Sample);
        AddAuthoredCase(sampleSuite, "sample one");
        TestSuite hiddenSuite = AttachSuite(setup, TestSuiteType.Hidden);
        AddAuthoredCase(hiddenSuite, "hidden one");
        AddAuthoredCase(hiddenSuite, "hidden two");

        _problemReadRepository
            .Setup(x => x.FindByIdForAdminAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<IEnumerable<StudioProblemDetailsSetupDto>> result = await _handler.Handle(
            new GetStudioProblemSetupsQuery(problem.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemDetailsSetupDto dto = result.Value.Single();
        Assert.Equal("2", dto.AmountOfTestSuites);
        Assert.Equal("3", dto.AmountOfTestCases);
        Assert.Single(dto.SampleTestCases);
    }
}