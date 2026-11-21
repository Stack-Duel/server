using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemVersioningTests
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

    private static ProblemSetup AddSetup(ProblemEntity problem) =>
        problem.AddSetup(Guid.NewGuid(), "def solve():", "solve", Guid.NewGuid());

    private static void UpdateTitle(ProblemEntity problem, string title) =>
        problem.UpdateContent(
            new Title(title),
            problem.Question,
            problem.Difficulty,
            problem.TimeLimit,
            problem.MemoryLimit
        );

    [Fact]
    public void NewProblem_HasNoVersions()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Empty(problem.Versions);
        Assert.Null(problem.PublishedVersionId);
        Assert.Null(problem.PublishedVersion);
    }

    [Fact]
    public void CompleteValidation_FreezesTheFirstVersion()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetup setup = AddSetup(problem);

        problem.SubmitForValidation();
        problem.CompleteValidation();

        ProblemVersion version = Assert.Single(problem.Versions);

        Assert.Equal(ProblemVersion.FirstVersionNumber, version.VersionNumber);
        Assert.Equal(ProblemVersionState.Published, version.State);
        Assert.Equal(problem.Id, version.ProblemId);
        Assert.Equal(version.Id, problem.PublishedVersionId);
        Assert.Equal("Two Sum", version.Title.Value);
        Assert.True(version.ReferencesSetup(setup.Id));
    }

    [Fact]
    public void FrozenVersion_KeepsTheStatementItWasPublishedWith()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();
        problem.CompleteValidation();

        UpdateTitle(problem, "Two Sum Revised");

        Assert.Equal("Two Sum Revised", problem.Title.Value);
        Assert.Equal("Two Sum", problem.PublishedVersion!.Title.Value);
    }

    [Fact]
    public void PublishingAgain_SupersedesThePreviousVersion()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();
        problem.CompleteValidation();
        ProblemVersion first = problem.PublishedVersion!;

        UpdateTitle(problem, "Two Sum Revised");
        problem.SubmitForValidation();
        problem.CompleteValidation();

        ProblemVersion second = problem.PublishedVersion!;

        Assert.Equal(2, problem.Versions.Count);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, second.VersionNumber);
        Assert.Equal(ProblemVersionState.Superseded, first.State);
        Assert.NotNull(first.SupersededAt);
        Assert.Equal(ProblemVersionState.Published, second.State);
        Assert.Equal("Two Sum Revised", second.Title.Value);
    }

    [Fact]
    public void FrozenVersion_ReferencesTheSetupsThatExistedAtPublishTime()
    {
        ProblemEntity problem = CreateProblem();
        ProblemSetup first = AddSetup(problem);

        problem.SubmitForValidation();
        problem.CompleteValidation();

        ProblemSetup added = AddSetup(problem);

        Assert.True(problem.PublishedVersion!.ReferencesSetup(first.Id));
        Assert.False(problem.PublishedVersion!.ReferencesSetup(added.Id));

        problem.SubmitForValidation();
        problem.CompleteValidation();

        Assert.True(problem.PublishedVersion!.ReferencesSetup(added.Id));
    }

    [Fact]
    public void Publish_FreezesAVersionToo()
    {
        ProblemEntity problem = CreateProblem();

        problem.Publish();

        Assert.Single(problem.Versions);
        Assert.Equal(ProblemStatus.Published, problem.Status);
    }

    [Fact]
    public void FindVersion_ReturnsASupersededVersionSoOldSubmissionsStayReadable()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();
        problem.CompleteValidation();
        Guid firstVersionId = problem.PublishedVersionId!.Value;

        UpdateTitle(problem, "Two Sum Revised");
        problem.SubmitForValidation();
        problem.CompleteValidation();

        ProblemVersion? pinned = problem.FindVersion(firstVersionId);

        Assert.NotNull(pinned);
        Assert.Equal("Two Sum", pinned.Title.Value);
    }

    [Fact]
    public void FindVersion_ReturnsNullForAnUnknownVersion()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();
        problem.CompleteValidation();

        Assert.Null(problem.FindVersion(Guid.NewGuid()));
    }

    [Fact]
    public void CancelValidation_DoesNotFreezeAVersion()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();

        problem.CancelValidation();

        Assert.Empty(problem.Versions);
    }

    [Fact]
    public void FailValidation_DoesNotFreezeAVersion()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();

        problem.FailValidation("Python: wrong answer");

        Assert.Empty(problem.Versions);
    }
}