using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites.Enums;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;
using ProblemSetupEntity = StackDuel.Domain.Problems.Entities.ProblemSetup;
using TestSuiteEntity = StackDuel.Domain.TestSuites.Entities.TestSuite;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemTests
{
    private static readonly Slug ValidSlug = new("two-sum");
    private static readonly Title ValidTitle = new("Two Sum");
    private static readonly Question ValidQuestion = new(new string('a', Question.MinLength));
    private static readonly Difficulty ValidDifficulty = new(500);
    private static readonly TimeLimit ValidTimeLimit = new(1000);
    private static readonly MemoryLimit ValidMemoryLimit = new(64);

    private static ProblemEntity CreateProblem() =>
        new(ValidSlug, ValidTitle, ValidQuestion, ValidDifficulty, ValidTimeLimit, ValidMemoryLimit);

    [Fact]
    public void Constructor_SetsSlug()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Equal(ValidSlug, problem.Slug);
    }

    [Fact]
    public void Constructor_SetsContentFields()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Equal(ValidTitle, problem.Title);
        Assert.Equal(ValidQuestion, problem.Question);
        Assert.Equal(ValidDifficulty, problem.Difficulty);
        Assert.Equal(ValidTimeLimit, problem.TimeLimit);
        Assert.Equal(ValidMemoryLimit, problem.MemoryLimit);
    }

    [Fact]
    public void Constructor_SetsStatusToDraft()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Equal(ProblemStatus.Draft, problem.Status);
    }

    [Fact]
    public void Constructor_SetsCreatedAt()
    {
        DateTime before = DateTime.UtcNow;
        ProblemEntity problem = CreateProblem();

        Assert.True(problem.CreatedAt >= before);
    }

    [Fact]
    public void Archive_SetsStatusToArchived()
    {
        ProblemEntity problem = CreateProblem();

        problem.Archive();

        Assert.Equal(ProblemStatus.Archived, problem.Status);
    }

    [Fact]
    public void Publish_SetsStatusToPublished()
    {
        ProblemEntity problem = CreateProblem();

        problem.Publish();

        Assert.Equal(ProblemStatus.Published, problem.Status);
    }

    [Fact]
    public void UpdateContent_UpdatesAllContentFields()
    {
        ProblemEntity problem = CreateProblem();
        Title newTitle = new("Three Sum");
        Question newQuestion = new(new string('b', Question.MinLength));
        Difficulty newDifficulty = new(1500);
        TimeLimit newTimeLimit = new(2000);
        MemoryLimit newMemoryLimit = new(128);

        problem.UpdateContent(newTitle, newQuestion, newDifficulty, newTimeLimit, newMemoryLimit);

        Assert.Equal(newTitle, problem.Title);
        Assert.Equal(newQuestion, problem.Question);
        Assert.Equal(newDifficulty, problem.Difficulty);
        Assert.Equal(newTimeLimit, problem.TimeLimit);
        Assert.Equal(newMemoryLimit, problem.MemoryLimit);
    }

    [Fact]
    public void UpdateContent_AddsHistoryEntry()
    {
        ProblemEntity problem = CreateProblem();

        problem.UpdateContent(
            new Title("Three Sum"),
            new Question(new string('b', Question.MinLength)),
            new Difficulty(1500),
            new TimeLimit(2000),
            new MemoryLimit(128)
        );

        Assert.Single(problem.History);
    }

    [Fact]
    public void UpdateContent_MultipleUpdates_AddsMultipleHistoryEntries()
    {
        ProblemEntity problem = CreateProblem();

        problem.UpdateContent(
            new Title("Three Sum"),
            new Question(new string('b', Question.MinLength)),
            new Difficulty(1500),
            new TimeLimit(2000),
            new MemoryLimit(128)
        );
        problem.UpdateContent(
            new Title("Four Sum"),
            new Question(new string('c', Question.MinLength)),
            new Difficulty(2500),
            new TimeLimit(1500),
            new MemoryLimit(256)
        );

        Assert.Equal(2, problem.History.Count);
    }

    [Fact]
    public void UpdateSlug_ChangesSlug()
    {
        ProblemEntity problem = CreateProblem();
        Slug newSlug = new("three-sum");

        problem.UpdateSlug(newSlug);

        Assert.Equal(newSlug, problem.Slug);
    }

    [Fact]
    public void AddSetup_AddsToSetups()
    {
        ProblemEntity problem = CreateProblem();

        problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        Assert.Single(problem.Setups);
    }

    [Fact]
    public void AddSetup_SetsProperties()
    {
        ProblemEntity problem = CreateProblem();
        Guid langVersionId = Guid.NewGuid();

        StackDuel.Domain.Problems.Entities.ProblemSetup setup = problem.AddSetup(
            langVersionId,
            "def twoSum():",
            "twoSum",
            Guid.NewGuid()
        );

        Assert.Equal(langVersionId, setup.LanguageVersionId);
        Assert.Equal("def twoSum():", setup.InitialCode);
        Assert.Equal("twoSum", setup.FunctionName);
    }

    private static ProblemSetupEntity AddSetupWithReferenceSolution(ProblemEntity problem)
    {
        ProblemSetupEntity setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());
        problem.SetReferenceSolution(setup.Id, "return 1");
        return setup;
    }

    private static TestSuiteEntity AttachTestSuite(ProblemSetupEntity setup, TestSuiteType type) =>
        ProblemSetupTestSuites.Attach(setup, type);

    private static void SetValidGenerationParameters(ProblemEntity problem) =>
        problem.SetGenerationParameters(
            [new GenerationParameterSpec("n", "int", 1, 10, null, null, null)],
            "int",
            25,
            99
        );

    [Fact]
    public void SubmitForValidation_SetsStatusToPending()
    {
        ProblemEntity problem = CreateProblem();

        problem.SubmitForValidation();

        Assert.Equal(ProblemStatus.Pending, problem.Status);
    }

    [Fact]
    public void SubmitForValidation_ClearsPreviousFailureReason()
    {
        ProblemEntity problem = CreateProblem();
        problem.FailValidation("reference solution timed out");

        problem.SubmitForValidation();

        Assert.Null(problem.ValidationFailureReason);
    }

    [Fact]
    public void FailValidation_SetsStatusAndReason()
    {
        ProblemEntity problem = CreateProblem();

        problem.FailValidation("reference solution timed out");

        Assert.Equal(ProblemStatus.Failed, problem.Status);
        Assert.Equal("reference solution timed out", problem.ValidationFailureReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FailValidation_BlankReason_Throws(string reason)
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.FailValidation(reason));
    }

    [Fact]
    public void FailValidation_BlankReason_LeavesStatusUnchanged()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.FailValidation(" "));

        Assert.Equal(ProblemStatus.Draft, problem.Status);
    }

    [Fact]
    public void CompleteValidation_PublishesAndClearsFailureReason()
    {
        ProblemEntity problem = CreateProblem();
        problem.FailValidation("reference solution timed out");

        problem.CompleteValidation();

        Assert.Equal(ProblemStatus.Published, problem.Status);
        Assert.Null(problem.ValidationFailureReason);
    }

    [Fact]
    public void SetTrack_AssignsTrackId()
    {
        ProblemEntity problem = CreateProblem();
        Guid trackId = Guid.NewGuid();

        problem.SetTrack(trackId);

        Assert.Equal(trackId, problem.TrackId);
    }

    [Fact]
    public void SetTrack_EmptyTrackId_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.SetTrack(Guid.Empty));
    }

    [Fact]
    public void SetCreatedBy_AssignsCreatedById()
    {
        ProblemEntity problem = CreateProblem();
        Guid userId = Guid.NewGuid();

        problem.SetCreatedBy(userId);

        Assert.Equal(userId, problem.CreatedById);
    }

    [Fact]
    public void SetCreatedBy_EmptyUserId_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.SetCreatedBy(Guid.Empty));
    }

    [Fact]
    public void SetGenerationParameters_BuildsGenerationSpec()
    {
        ProblemEntity problem = CreateProblem();

        SetValidGenerationParameters(problem);

        Assert.NotNull(problem.GenerationSpec);
        Assert.Equal("int", problem.GenerationSpec.OutputValueType);
        Assert.Equal(25, problem.GenerationSpec.TargetCaseCount);
        Assert.Equal(99, problem.GenerationSpec.Seed);
        Assert.Equal("n", Assert.Single(problem.GenerationSpec.Parameters).Name);
    }

    [Fact]
    public void SetReferenceSolution_AssignsCodeToSetup()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        problem.SetReferenceSolution(setup.Id, "return 1");

        Assert.Equal("return 1", setup.ReferenceSolutionCode);
    }

    [Fact]
    public void SetReferenceSolution_UnknownSetup_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<InvalidOperationException>(() => problem.SetReferenceSolution(Guid.NewGuid(), "return 1"));
    }

    [Fact]
    public void SetGenerationSpecId_AssignsSpecIdToSetup()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());
        Guid specId = Guid.NewGuid();

        problem.SetGenerationSpecId(setup.Id, specId);

        Assert.Equal(specId, setup.GenerationSpecId);
    }

    [Fact]
    public void SetGenerationSpecId_UnknownSetup_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<InvalidOperationException>(() => problem.SetGenerationSpecId(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void SetAdditionalFileBundle_AssignsBundleIdToSetup()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());
        Guid bundleId = Guid.NewGuid();

        problem.SetAdditionalFileBundle(setup.Id, bundleId);

        Assert.Equal(bundleId, setup.AdditionalFileBundleId);
    }

    [Fact]
    public void SetAdditionalFileBundle_UnknownSetup_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<InvalidOperationException>(() => problem.SetAdditionalFileBundle(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void UpdateSetupInitialCode_ReplacesInitialCode()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        problem.UpdateSetupInitialCode(setup.Id, "def threeSum():");

        Assert.Equal("def threeSum():", setup.InitialCode);
    }

    [Fact]
    public void UpdateSetupInitialCode_UnknownSetup_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<InvalidOperationException>(() => problem.UpdateSetupInitialCode(Guid.NewGuid(), "code"));
    }

    [Fact]
    public void UpdateSetupFunctionName_ReplacesFunctionName()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        problem.UpdateSetupFunctionName(setup.Id, "threeSum");

        Assert.Equal("threeSum", setup.FunctionName);
    }

    [Fact]
    public void UpdateSetupFunctionName_UnknownSetup_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<InvalidOperationException>(() => problem.UpdateSetupFunctionName(Guid.NewGuid(), "threeSum"));
    }

    [Fact]
    public void AvailableLanguageVersionIds_ReturnsEverySetupLanguageVersion()
    {
        ProblemEntity problem = CreateProblem();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        problem.AddSetup(first, "def twoSum():", "twoSum", Guid.NewGuid());
        problem.AddSetup(second, "function twoSum() {}", "twoSum", Guid.NewGuid());

        Assert.Equal([first, second], problem.AvailableLanguageVersionIds());
    }

    [Fact]
    public void FindSetupByLanguageVersionId_MatchingSetup_ReturnsIt()
    {
        ProblemEntity problem = CreateProblem();
        Guid languageVersionId = Guid.NewGuid();
        ProblemSetupEntity setup = problem.AddSetup(languageVersionId, "def twoSum():", "twoSum", Guid.NewGuid());

        Assert.Same(setup, problem.FindSetupByLanguageVersionId(languageVersionId));
    }

    [Fact]
    public void FindSetupByLanguageVersionId_NoMatchingSetup_ReturnsNull()
    {
        ProblemEntity problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        Assert.Null(problem.FindSetupByLanguageVersionId(Guid.NewGuid()));
    }

    [Fact]
    public void SetupsMissingReferenceSolution_NoSetups_ReturnsEmpty()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Empty(problem.SetupsMissingReferenceSolution());
    }

    [Fact]
    public void SetupsMissingReferenceSolution_ReturnsOnlySetupsWithoutSolution()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity missing = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());
        AddSetupWithReferenceSolution(problem);

        Assert.Same(missing, Assert.Single(problem.SetupsMissingReferenceSolution()));
    }

    [Fact]
    public void EvaluatePublishReadiness_NewProblem_ReportsNothingSatisfied()
    {
        ProblemEntity problem = CreateProblem();

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        Assert.Equal(0, readiness.SetupCount);
        Assert.Equal(0, readiness.SetupsMissingReferenceSolution);
        Assert.Equal(0, readiness.SampleTestCaseCount);
        Assert.False(readiness.HasGenerationParameters);
        Assert.Equal(0, readiness.RequirementsMet);
        Assert.False(readiness.IsReady);
    }

    [Fact]
    public void EvaluatePublishReadiness_SetupWithoutReferenceSolution_CountsItAsMissing()
    {
        ProblemEntity problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        Assert.Equal(1, readiness.SetupCount);
        Assert.Equal(1, readiness.SetupsMissingReferenceSolution);
        Assert.True(readiness.HasSetups);
        Assert.False(readiness.HasReferenceSolutions);
    }

    [Fact]
    public void EvaluatePublishReadiness_EverySetupHasReferenceSolution_ReportsRequirementMet()
    {
        ProblemEntity problem = CreateProblem();
        AddSetupWithReferenceSolution(problem);
        AddSetupWithReferenceSolution(problem);

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        Assert.Equal(2, readiness.SetupCount);
        Assert.Equal(0, readiness.SetupsMissingReferenceSolution);
        Assert.True(readiness.HasReferenceSolutions);
    }

    [Fact]
    public void EvaluatePublishReadiness_GenerationParametersSet_ReportsRequirementMet()
    {
        ProblemEntity problem = CreateProblem();
        SetValidGenerationParameters(problem);

        Assert.True(problem.EvaluatePublishReadiness().HasGenerationParameters);
    }

    [Fact]
    public void EvaluatePublishReadiness_CountsAuthoredSampleTestCases()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = AddSetupWithReferenceSolution(problem);
        TestSuiteEntity sampleSuite = AttachTestSuite(setup, TestSuiteType.Sample);
        sampleSuite.AddTestCase("first");
        sampleSuite.AddTestCase("second");

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        Assert.Equal(2, readiness.SampleTestCaseCount);
        Assert.True(readiness.HasSampleTestCases);
    }

    [Fact]
    public void EvaluatePublishReadiness_IgnoresHiddenSuiteTestCases()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = AddSetupWithReferenceSolution(problem);
        AttachTestSuite(setup, TestSuiteType.Hidden).AddTestCase("hidden case");

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        Assert.Equal(0, readiness.SampleTestCaseCount);
        Assert.False(readiness.HasSampleTestCases);
    }

    [Fact]
    public void EvaluatePublishReadiness_IgnoresGeneratedSampleTestCases()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = AddSetupWithReferenceSolution(problem);
        AttachTestSuite(setup, TestSuiteType.Sample).AddGeneratedTestCase("generated", Guid.NewGuid(), 0);

        Assert.Equal(0, problem.EvaluatePublishReadiness().SampleTestCaseCount);
    }

    [Fact]
    public void EvaluatePublishReadiness_IgnoresRetiredSampleTestCases()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = AddSetupWithReferenceSolution(problem);
        TestSuiteEntity sampleSuite = AttachTestSuite(setup, TestSuiteType.Sample);
        sampleSuite.AddTestCase("live");
        sampleSuite.AddTestCase("retired").Retire();

        Assert.Equal(1, problem.EvaluatePublishReadiness().SampleTestCaseCount);
    }

    [Fact]
    public void EvaluatePublishReadiness_EveryRequirementSatisfied_IsReady()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetupEntity setup = AddSetupWithReferenceSolution(problem);
        AttachTestSuite(setup, TestSuiteType.Sample).AddTestCase("first");
        SetValidGenerationParameters(problem);

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        Assert.True(readiness.IsReady);
        Assert.Equal(ProblemPublishReadiness.RequirementCount, readiness.RequirementsMet);
    }

    [Fact]
    public void ResolveSubmittedStatuses_ContainsTerminalStatusesOnly()
    {
        Assert.Equal([ProblemStatus.Published, ProblemStatus.Archived], ProblemEntity.ResolveSubmittedStatuses.Order());
    }

    [Theory]
    [InlineData(ProblemStatus.Draft)]
    [InlineData(ProblemStatus.Pending)]
    [InlineData(ProblemStatus.Failed)]
    public void ResolveSubmittedStatuses_ExcludesInProgressStatuses(ProblemStatus status)
    {
        Assert.DoesNotContain(status, ProblemEntity.ResolveSubmittedStatuses);
    }

    [Fact]
    public void TagCountBounds_AllowBetweenOneAndFiveTags()
    {
        Assert.Equal(1, ProblemEntity.MinTagCount);
        Assert.Equal(5, ProblemEntity.MaxTagCount);
    }

    [Fact]
    public void MaxOpenProblemsPerCreator_IsTen()
    {
        Assert.Equal(10, ProblemEntity.MaxOpenProblemsPerCreator);
    }
}