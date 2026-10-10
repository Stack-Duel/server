using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;

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
            new TimeLimit(3000),
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
}