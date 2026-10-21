using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Domain.Tests.TestSuites.Entities;

public class TestCaseTests
{
    [Fact]
    public void AddTestCase_DefaultsToAuthoredSource()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);

        var testCase = suite.AddTestCase("Case 1");

        Assert.Equal(TestCaseSource.Authored, testCase.Source);
        Assert.Null(testCase.GenerationSpecId);
        Assert.Null(testCase.GenerationCaseIndex);
    }

    [Fact]
    public void AddInput_AssignsPositionsInCallOrder()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        var testCase = suite.AddTestCase("Case 1");

        var nums = testCase.AddInput("[2,7,11,15]", "integer_array");
        var target = testCase.AddInput("9", "integer");

        Assert.Equal(0, nums.Position);
        Assert.Equal(1, target.Position);
    }

    [Fact]
    public void AddGeneratedTestCase_SetsGeneratedSourceAndRecipe()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        Guid specId = Guid.NewGuid();

        var testCase = suite.AddGeneratedTestCase("Generated 3", specId, 3);

        Assert.Equal(TestCaseSource.Generated, testCase.Source);
        Assert.Equal(specId, testCase.GenerationSpecId);
        Assert.Equal(3, testCase.GenerationCaseIndex);
        Assert.Empty(testCase.Inputs); // generated cases store a recipe, not materialized inputs
    }

    [Fact]
    public void ClearGeneratedTestCases_RemovesOnlyGeneratedCases()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        suite.AddTestCase("Authored 1");
        suite.AddGeneratedTestCase("Generated 1", Guid.NewGuid(), 0);
        suite.AddGeneratedTestCase("Generated 2", Guid.NewGuid(), 1);

        suite.ClearGeneratedTestCases();

        Assert.Single(suite.TestCases);
        Assert.Equal(TestCaseSource.Authored, suite.TestCases.Single().Source);
    }

    [Fact]
    public void Retire_SetsRetiredAt()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        var testCase = suite.AddGeneratedTestCase("Generated 1", Guid.NewGuid(), 0);

        testCase.Retire();

        Assert.NotNull(testCase.RetiredAt);
    }

    [Fact]
    public void Retire_CalledTwice_KeepsOriginalTimestamp()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        var testCase = suite.AddGeneratedTestCase("Generated 1", Guid.NewGuid(), 0);

        testCase.Retire();
        DateTime? firstRetiredAt = testCase.RetiredAt;
        testCase.Retire();

        Assert.Equal(firstRetiredAt, testCase.RetiredAt);
    }
}
public class TestCasePositionTests
{
    [Fact]
    public void AddTestCase_DefaultsToPositionZero()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Sample);

        var testCase = suite.AddTestCase("Case 1");

        Assert.Equal(0, testCase.Position);
    }

    [Fact]
    public void AddTestCase_KeepsTheGivenPosition()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Sample);

        var second = suite.AddTestCase("Case 2", position: 1);
        var third = suite.AddTestCase("Case 3", position: 2);

        Assert.Equal(1, second.Position);
        Assert.Equal(2, third.Position);
    }

    [Fact]
    public void AddTestCase_NegativePosition_Throws()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Sample);

        Assert.Throws<ArgumentException>(() => suite.AddTestCase("Case 1", position: -1));
    }

    [Fact]
    public void OrderingByPosition_RecoversTheAuthoredOrderRegardlessOfInsertionOrder()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Sample);
        suite.AddTestCase("third", position: 2);
        suite.AddTestCase("first", position: 0);
        suite.AddTestCase("second", position: 1);

        var ordered = suite.TestCases.OrderBy(tc => tc.Position).Select(tc => tc.Name).ToList();

        Assert.Equal(["first", "second", "third"], ordered);
    }
}