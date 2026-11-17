using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class ProblemPublishReadinessTests
{
    private static ProblemPublishReadiness FullyReady() => new(1, 0, 1, true);

    [Fact]
    public void RequirementCount_IsFour()
    {
        Assert.Equal(4, ProblemPublishReadiness.RequirementCount);
    }

    [Fact]
    public void HasSetups_NoSetups_IsFalse()
    {
        ProblemPublishReadiness readiness = new(0, 0, 1, true);

        Assert.False(readiness.HasSetups);
    }

    [Fact]
    public void HasSetups_AtLeastOneSetup_IsTrue()
    {
        ProblemPublishReadiness readiness = new(1, 0, 1, true);

        Assert.True(readiness.HasSetups);
    }

    [Fact]
    public void HasReferenceSolutions_NoSetups_IsFalse()
    {
        ProblemPublishReadiness readiness = new(0, 0, 1, true);

        Assert.False(readiness.HasReferenceSolutions);
    }

    [Fact]
    public void HasReferenceSolutions_SomeSetupsMissingSolution_IsFalse()
    {
        ProblemPublishReadiness readiness = new(2, 1, 1, true);

        Assert.False(readiness.HasReferenceSolutions);
    }

    [Fact]
    public void HasReferenceSolutions_EverySetupHasSolution_IsTrue()
    {
        ProblemPublishReadiness readiness = new(2, 0, 1, true);

        Assert.True(readiness.HasReferenceSolutions);
    }

    [Fact]
    public void HasSampleTestCases_NoSampleCases_IsFalse()
    {
        ProblemPublishReadiness readiness = new(1, 0, 0, true);

        Assert.False(readiness.HasSampleTestCases);
    }

    [Fact]
    public void HasSampleTestCases_AtLeastOneSampleCase_IsTrue()
    {
        ProblemPublishReadiness readiness = new(1, 0, 3, true);

        Assert.True(readiness.HasSampleTestCases);
    }

    [Fact]
    public void RequirementsMet_NothingSatisfied_IsZero()
    {
        ProblemPublishReadiness readiness = new(0, 0, 0, false);

        Assert.Equal(0, readiness.RequirementsMet);
    }

    [Fact]
    public void RequirementsMet_EverythingSatisfied_EqualsRequirementCount()
    {
        Assert.Equal(ProblemPublishReadiness.RequirementCount, FullyReady().RequirementsMet);
    }

    [Theory]
    [InlineData(0, 0, 0, true, 1)]
    [InlineData(0, 0, 2, false, 1)]
    [InlineData(1, 1, 0, false, 1)]
    [InlineData(1, 0, 0, false, 2)]
    [InlineData(1, 1, 2, true, 3)]
    [InlineData(2, 0, 2, false, 3)]
    public void RequirementsMet_CountsOnlySatisfiedRequirements(
        int setupCount,
        int setupsMissingReferenceSolution,
        int sampleTestCaseCount,
        bool hasGenerationParameters,
        int expected
    )
    {
        ProblemPublishReadiness readiness = new(
            setupCount,
            setupsMissingReferenceSolution,
            sampleTestCaseCount,
            hasGenerationParameters
        );

        Assert.Equal(expected, readiness.RequirementsMet);
    }

    [Fact]
    public void IsReady_EveryRequirementMet_IsTrue()
    {
        Assert.True(FullyReady().IsReady);
    }

    [Fact]
    public void IsReady_MissingGenerationParameters_IsFalse()
    {
        ProblemPublishReadiness readiness = new(1, 0, 1, false);

        Assert.False(readiness.IsReady);
    }

    [Fact]
    public void IsReady_MissingReferenceSolution_IsFalse()
    {
        ProblemPublishReadiness readiness = new(1, 1, 1, true);

        Assert.False(readiness.IsReady);
    }

    [Fact]
    public void IsReady_MissingSampleTestCases_IsFalse()
    {
        ProblemPublishReadiness readiness = new(1, 0, 0, true);

        Assert.False(readiness.IsReady);
    }

    [Fact]
    public void IsReady_NoSetups_IsFalse()
    {
        ProblemPublishReadiness readiness = new(0, 0, 1, true);

        Assert.False(readiness.IsReady);
    }
}
