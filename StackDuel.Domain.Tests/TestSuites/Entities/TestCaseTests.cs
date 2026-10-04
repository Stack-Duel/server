using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Domain.Tests.TestSuites.Entities;

public class TestCaseTests
{
    [Test]
    public void AddTestCase_DefaultsToAuthoredSource()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);

        var testCase = suite.AddTestCase("Case 1");

        Assert.That(testCase.Source, Is.EqualTo(TestCaseSource.Authored));
        Assert.That(testCase.GenerationSpecId, Is.Null);
        Assert.That(testCase.GenerationCaseIndex, Is.Null);
    }

    [Test]
    public void AddInput_AssignsPositionsInCallOrder()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        var testCase = suite.AddTestCase("Case 1");

        var nums = testCase.AddInput("[2,7,11,15]", "integer_array");
        var target = testCase.AddInput("9", "integer");

        Assert.That(nums.Position, Is.EqualTo(0));
        Assert.That(target.Position, Is.EqualTo(1));
    }

    [Test]
    public void AddGeneratedTestCase_SetsGeneratedSourceAndRecipe()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        Guid specId = Guid.NewGuid();

        var testCase = suite.AddGeneratedTestCase("Generated 3", specId, 3);

        Assert.That(testCase.Source, Is.EqualTo(TestCaseSource.Generated));
        Assert.That(testCase.GenerationSpecId, Is.EqualTo(specId));
        Assert.That(testCase.GenerationCaseIndex, Is.EqualTo(3));
        Assert.That(testCase.Inputs, Is.Empty, "generated cases store a recipe, not materialized inputs");
    }

    [Test]
    public void ClearGeneratedTestCases_RemovesOnlyGeneratedCases()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        suite.AddTestCase("Authored 1");
        suite.AddGeneratedTestCase("Generated 1", Guid.NewGuid(), 0);
        suite.AddGeneratedTestCase("Generated 2", Guid.NewGuid(), 1);

        suite.ClearGeneratedTestCases();

        Assert.That(suite.TestCases, Has.Count.EqualTo(1));
        Assert.That(suite.TestCases.Single().Source, Is.EqualTo(TestCaseSource.Authored));
    }

    [Test]
    public void Retire_SetsRetiredAt()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        var testCase = suite.AddGeneratedTestCase("Generated 1", Guid.NewGuid(), 0);

        testCase.Retire();

        Assert.That(testCase.RetiredAt, Is.Not.Null);
    }

    [Test]
    public void Retire_CalledTwice_KeepsOriginalTimestamp()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Hidden);
        var testCase = suite.AddGeneratedTestCase("Generated 1", Guid.NewGuid(), 0);

        testCase.Retire();
        DateTime? firstRetiredAt = testCase.RetiredAt;
        testCase.Retire();

        Assert.That(testCase.RetiredAt, Is.EqualTo(firstRetiredAt));
    }
}