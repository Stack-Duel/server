using Ardalis.Result;
using Moq;
using StackDuel.Application.Commands.Problems.SetProblemSetups;
using StackDuel.Application.Languages;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using SetProblemSetupsCommand = StackDuel.Application.Commands.Problems.SetProblemSetups.SetProblemSetupsCommand;
using SetProblemSetupsHandler = StackDuel.Application.Commands.Problems.SetProblemSetups.SetProblemSetupsHandler;
using SetProblemSetupsValidator = StackDuel.Application.Commands.Problems.SetProblemSetups.SetProblemSetupsValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemSetups;

public class SetProblemSetupsHandlerTests
{
    private static readonly string ValidQuestion = new('q', 60);
    private static readonly Guid PipelineId = Guid.NewGuid();

    private Mock<IProblemRepository> _problemRepository = null!;
    private Mock<IExecutionPipelineRepository> _executionPipelineRepository = null!;
    private Mock<ILanguageVersionResolver> _languageVersionResolver = null!;
    private SetProblemSetupsHandler _handler = null!;

    public SetProblemSetupsHandlerTests()
    {
        _problemRepository = new Mock<IProblemRepository>();
        _executionPipelineRepository = new Mock<IExecutionPipelineRepository>();
        _languageVersionResolver = new Mock<ILanguageVersionResolver>();

        _executionPipelineRepository
            .Setup(x => x.FindIdByNameAsync(WellKnownExecutionPipelines.DefaultName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PipelineId);

        _languageVersionResolver
            .Setup(x => x.ResolveAsync(It.IsAny<LanguageVersionReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (LanguageVersionReference reference, CancellationToken _) =>
                    reference.LanguageVersionId is { } id && id != Guid.Empty
                        ? Result<Guid>.Success(id)
                        : Result<Guid>.Invalid(new ValidationError("Unknown language."))
            );

        _handler = new SetProblemSetupsHandler(
            new SetProblemSetupsValidator(),
            _problemRepository.Object,
            _executionPipelineRepository.Object,
            _languageVersionResolver.Object
        );
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(ValidQuestion),
            new Difficulty(100),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    private void GivenProblem(Problem problem) =>
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

    private void GivenLanguageResolves(string language, Guid languageVersionId) =>
        _languageVersionResolver
            .Setup(x =>
                x.ResolveAsync(
                    It.Is<LanguageVersionReference>(reference => reference.Language == language),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<Guid>.Success(languageVersionId));

    private static ProblemSetupInputDto ById(Guid languageVersionId) =>
        new(languageVersionId, null, null, "starter", "solve", "reference");

    private static ProblemSetupInputDto ByName(string language, string? version = null) =>
        new(null, language, version, "starter", "solve", "reference");

    private static SetProblemSetupsCommand Command(Guid problemId, params ProblemSetupInputDto[] setups) =>
        new(problemId, setups);

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(Command(Guid.NewGuid(), ById(Guid.NewGuid())), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemPendingValidation_ReturnsInvalidAndDoesNotPersist()
    {
        var problem = CreateProblem();
        problem.SubmitForValidation();
        GivenProblem(problem);

        Result result = await _handler.Handle(Command(problem.Id, ById(Guid.NewGuid())), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoDefaultPipeline_ReturnsErrorAndDoesNotPersist()
    {
        var problem = CreateProblem();
        GivenProblem(problem);
        _executionPipelineRepository
            .Setup(x => x.FindIdByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        Result result = await _handler.Handle(Command(problem.Id, ById(Guid.NewGuid())), CancellationToken.None);

        Assert.Equal(ResultStatus.Error, result.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownLanguageVersion_AddsSetupWithReferenceSolution()
    {
        var problem = CreateProblem();
        GivenProblem(problem);
        Guid languageVersionId = Guid.NewGuid();

        Result result = await _handler.Handle(Command(problem.Id, ById(languageVersionId)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProblemSetup? setup = problem.FindSetupByLanguageVersionId(languageVersionId);
        Assert.NotNull(setup);
        Assert.Equal("starter", setup.InitialCode);
        Assert.Equal("solve", setup.FunctionName);
        Assert.Equal("reference", setup.ReferenceSolutionCode);
    }

    [Fact]
    public async Task Handle_LanguageNamedBySlug_ResolvesToItsVersionAndAddsSetup()
    {
        var problem = CreateProblem();
        GivenProblem(problem);
        Guid pythonVersionId = Guid.NewGuid();
        GivenLanguageResolves("python", pythonVersionId);

        Result result = await _handler.Handle(Command(problem.Id, ByName("python")), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(problem.FindSetupByLanguageVersionId(pythonVersionId));
    }

    [Fact]
    public async Task Handle_LanguageNamedWithVersion_PassesBothToTheResolver()
    {
        var problem = CreateProblem();
        GivenProblem(problem);
        GivenLanguageResolves("python", Guid.NewGuid());

        await _handler.Handle(Command(problem.Id, ByName("python", "3.13.2")), CancellationToken.None);

        _languageVersionResolver.Verify(
            x =>
                x.ResolveAsync(
                    It.Is<LanguageVersionReference>(reference =>
                        reference.Language == "python" && reference.Version == "3.13.2"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_UnresolvableLanguage_ReturnsInvalidAndDoesNotPersist()
    {
        var problem = CreateProblem();
        GivenProblem(problem);

        Result result = await _handler.Handle(Command(problem.Id, ByName("nope")), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LanguageNameAndIdPointingAtTheSameVersion_IsRejectedAsDuplicate()
    {
        var problem = CreateProblem();
        GivenProblem(problem);
        Guid pythonVersionId = Guid.NewGuid();
        GivenLanguageResolves("python", pythonVersionId);

        Result result = await _handler.Handle(
            Command(problem.Id, ById(pythonVersionId), ByName("python")),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingLanguageVersion_UpdatesSetupInPlace()
    {
        var problem = CreateProblem();
        Guid languageVersionId = Guid.NewGuid();
        problem.AddSetup(languageVersionId, "old code", "oldName", PipelineId);
        GivenProblem(problem);

        Result result = await _handler.Handle(
            Command(
                problem.Id,
                new ProblemSetupInputDto(languageVersionId, null, null, "new code", "newName", "new reference")
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Single(problem.Setups);
        ProblemSetup setup = problem.Setups.Single();
        Assert.Equal("new code", setup.InitialCode);
        Assert.Equal("newName", setup.FunctionName);
        Assert.Equal("new reference", setup.ReferenceSolutionCode);
    }

    [Fact]
    public async Task Handle_MultipleSetups_PersistsOnceForAllOfThem()
    {
        var problem = CreateProblem();
        Guid existingLanguageVersionId = Guid.NewGuid();
        Guid newLanguageVersionId = Guid.NewGuid();
        problem.AddSetup(existingLanguageVersionId, "old code", "oldName", PipelineId);
        GivenProblem(problem);

        Result result = await _handler.Handle(
            Command(
                problem.Id,
                new ProblemSetupInputDto(
                    existingLanguageVersionId,
                    null,
                    null,
                    "updated",
                    "updatedName",
                    "updated reference"
                ),
                new ProblemSetupInputDto(newLanguageVersionId, null, null, "added", null, "added reference")
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, problem.Setups.Count);
        Assert.Equal("updated", problem.FindSetupByLanguageVersionId(existingLanguageVersionId)!.InitialCode);
        Assert.Equal(
            "added reference",
            problem.FindSetupByLanguageVersionId(newLanguageVersionId)!.ReferenceSolutionCode
        );
        _problemRepository.Verify(x => x.UpdateAsync(problem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SetupsNotListed_AreLeftUntouched()
    {
        var problem = CreateProblem();
        Guid untouchedLanguageVersionId = Guid.NewGuid();
        problem.AddSetup(untouchedLanguageVersionId, "keep me", "keepName", PipelineId);
        GivenProblem(problem);

        Result result = await _handler.Handle(Command(problem.Id, ById(Guid.NewGuid())), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProblemSetup untouched = problem.FindSetupByLanguageVersionId(untouchedLanguageVersionId)!;
        Assert.Equal("keep me", untouched.InitialCode);
        Assert.Equal("keepName", untouched.FunctionName);
    }
}