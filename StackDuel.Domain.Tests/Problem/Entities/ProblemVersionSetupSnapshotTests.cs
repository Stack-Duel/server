using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemVersionSetupSnapshotTests
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

    [Fact]
    public void FrozenVersion_SnapshotsTheStarterCodeAndFunctionName()
    {
        ProblemEntity problem = CreateProblem();
        Guid languageVersionId = Guid.NewGuid();
        ProblemSetup setup = problem.AddSetup(languageVersionId, "def solve(): pass", "solve", Guid.NewGuid());

        problem.SubmitForValidation();
        problem.CompleteValidation();

        ProblemVersionSetup pinned = Assert.Single(problem.PublishedVersion!.Setups);

        Assert.Equal(setup.Id, pinned.ProblemSetupId);
        Assert.Equal(languageVersionId, pinned.LanguageVersionId);
        Assert.Equal("def solve(): pass", pinned.InitialCode);
        Assert.Equal("solve", pinned.FunctionName);
    }

    [Fact]
    public void EditingASetupDoesNotChangeWhatThePublishedVersionServes()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "def solve():", "solve", Guid.NewGuid());
        problem.SubmitForValidation();
        problem.CompleteValidation();

        problem.UpdateSetupInitialCode(setup.Id, "def solve(): return 42");
        problem.UpdateSetupFunctionName(setup.Id, "renamed");

        ProblemVersionSetup pinned = Assert.Single(problem.PublishedVersion!.Setups);

        Assert.Equal("def solve():", pinned.InitialCode);
        Assert.Equal("solve", pinned.FunctionName);
        Assert.Equal("def solve(): return 42", setup.InitialCode);
    }

    [Fact]
    public void PublishingAgain_PicksUpTheEditedStarterCode()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "def solve():", "solve", Guid.NewGuid());
        problem.SubmitForValidation();
        problem.CompleteValidation();

        problem.UpdateSetupInitialCode(setup.Id, "def solve(): return 42");
        problem.SubmitForValidation();
        problem.CompleteValidation();

        Assert.Equal("def solve(): return 42", Assert.Single(problem.PublishedVersion!.Setups).InitialCode);
    }

    [Fact]
    public void FindSetupByLanguageVersionId_ResolvesThePinnedSetup()
    {
        ProblemEntity problem = CreateProblem();
        Guid languageVersionId = Guid.NewGuid();
        problem.AddSetup(languageVersionId, "starter", "solve", Guid.NewGuid());
        problem.SubmitForValidation();
        problem.CompleteValidation();

        Assert.NotNull(problem.PublishedVersion!.FindSetupByLanguageVersionId(languageVersionId));
        Assert.Null(problem.PublishedVersion!.FindSetupByLanguageVersionId(Guid.NewGuid()));
    }

    [Fact]
    public void FrozenVersion_KeepsOneEntryPerSetup()
    {
        ProblemEntity problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "starter one", "solve", Guid.NewGuid());
        problem.AddSetup(Guid.NewGuid(), "starter two", "solve", Guid.NewGuid());

        problem.SubmitForValidation();
        problem.CompleteValidation();

        Assert.Equal(2, problem.PublishedVersion!.Setups.Count);
    }
}