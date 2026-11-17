using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Enums;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;
using ProblemSetupEntity = StackDuel.Domain.Problems.Entities.ProblemSetup;
using TestSuiteEntity = StackDuel.Domain.TestSuites.Entities.TestSuite;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemSetupTests
{
    private static ProblemEntity CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('a', Question.MinLength)),
            new Difficulty(500),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private static ProblemSetupEntity CreateSetup(
        string initialCode = "def twoSum():",
        string? functionName = "twoSum"
    ) => CreateProblem().AddSetup(Guid.NewGuid(), initialCode, functionName, Guid.NewGuid());

    [Fact]
    public void Constructor_EmptyLanguageVersionId_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.AddSetup(Guid.Empty, "def twoSum():", "twoSum", Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankInitialCode_Throws(string initialCode)
    {
        Assert.Throws<ArgumentException>(() => CreateSetup(initialCode));
    }

    [Fact]
    public void Constructor_BlankFunctionName_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSetup(functionName: "   "));
    }

    [Fact]
    public void Constructor_NullFunctionName_IsAllowed()
    {
        Assert.Null(CreateSetup(functionName: null).FunctionName);
    }

    [Fact]
    public void Constructor_EmptyPipelineId_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.Empty));
    }

    [Fact]
    public void Constructor_LeavesOptionalReferencesUnset()
    {
        ProblemSetupEntity setup = CreateSetup();

        Assert.Null(setup.ReferenceSolutionCode);
        Assert.Null(setup.GenerationSpecId);
        Assert.Null(setup.AdditionalFileBundleId);
        Assert.Empty(setup.AdditionalFiles);
        Assert.Empty(setup.TestSuites);
    }

    [Fact]
    public void UpdateInitialCode_ReplacesCode()
    {
        ProblemSetupEntity setup = CreateSetup();

        setup.UpdateInitialCode("def threeSum():");

        Assert.Equal("def threeSum():", setup.InitialCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateInitialCode_BlankCode_Throws(string initialCode)
    {
        ProblemSetupEntity setup = CreateSetup();

        Assert.Throws<ArgumentException>(() => setup.UpdateInitialCode(initialCode));
    }

    [Fact]
    public void UpdateFunctionName_ReplacesName()
    {
        ProblemSetupEntity setup = CreateSetup();

        setup.UpdateFunctionName("threeSum");

        Assert.Equal("threeSum", setup.FunctionName);
    }

    [Fact]
    public void UpdateFunctionName_Null_ClearsName()
    {
        ProblemSetupEntity setup = CreateSetup();

        setup.UpdateFunctionName(null);

        Assert.Null(setup.FunctionName);
    }

    [Fact]
    public void UpdateFunctionName_BlankName_Throws()
    {
        ProblemSetupEntity setup = CreateSetup();

        Assert.Throws<ArgumentException>(() => setup.UpdateFunctionName("   "));
    }

    [Fact]
    public void SetAdditionalFiles_ReplacesFileList()
    {
        ProblemSetupEntity setup = CreateSetup();
        ProblemSetupFile helper = new("helper.py", "def helper(): pass");

        setup.SetAdditionalFiles([helper]);

        Assert.Equal(helper, Assert.Single(setup.AdditionalFiles));
    }

    [Fact]
    public void SetAdditionalFiles_CalledAgain_DiscardsPreviousFiles()
    {
        ProblemSetupEntity setup = CreateSetup();
        setup.SetAdditionalFiles([new ProblemSetupFile("helper.py", "def helper(): pass")]);

        setup.SetAdditionalFiles([new ProblemSetupFile("other.py", "def other(): pass")]);

        Assert.Equal("other.py", Assert.Single(setup.AdditionalFiles).Path);
    }

    [Fact]
    public void PublicTestSuites_NoTestSuites_ReturnsEmpty()
    {
        Assert.Empty(CreateSetup().PublicTestSuites());
    }

    [Fact]
    public void PublicTestSuites_ReturnsOnlySampleSuites()
    {
        ProblemSetupEntity setup = CreateSetup();
        TestSuiteEntity sampleSuite = ProblemSetupTestSuites.Attach(setup, TestSuiteType.Sample);
        ProblemSetupTestSuites.Attach(setup, TestSuiteType.Hidden);

        Assert.Same(sampleSuite, Assert.Single(setup.PublicTestSuites()));
    }
}
