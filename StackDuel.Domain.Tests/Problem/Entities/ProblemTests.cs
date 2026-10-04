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

    [Test]
    public void Constructor_SetsSlug()
    {
        ProblemEntity problem = CreateProblem();

        Assert.That(problem.Slug, Is.EqualTo(ValidSlug));
    }

    [Test]
    public void Constructor_SetsContentFields()
    {
        ProblemEntity problem = CreateProblem();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Title, Is.EqualTo(ValidTitle));
            Assert.That(problem.Question, Is.EqualTo(ValidQuestion));
            Assert.That(problem.Difficulty, Is.EqualTo(ValidDifficulty));
            Assert.That(problem.TimeLimit, Is.EqualTo(ValidTimeLimit));
            Assert.That(problem.MemoryLimit, Is.EqualTo(ValidMemoryLimit));
        }
    }

    [Test]
    public void Constructor_SetsStatusToDraft()
    {
        ProblemEntity problem = CreateProblem();

        Assert.That(problem.Status, Is.EqualTo(ProblemStatus.Draft));
    }

    [Test]
    public void Constructor_SetsCreatedAt()
    {
        DateTime before = DateTime.UtcNow;
        ProblemEntity problem = CreateProblem();

        Assert.That(problem.CreatedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Archive_SetsStatusToArchived()
    {
        ProblemEntity problem = CreateProblem();

        problem.Archive();

        Assert.That(problem.Status, Is.EqualTo(ProblemStatus.Archived));
    }

    [Test]
    public void Publish_SetsStatusToPublished()
    {
        ProblemEntity problem = CreateProblem();

        problem.Publish();

        Assert.That(problem.Status, Is.EqualTo(ProblemStatus.Published));
    }

    [Test]
    public void UpdateContent_UpdatesAllContentFields()
    {
        ProblemEntity problem = CreateProblem();
        Title newTitle = new("Three Sum");
        Question newQuestion = new(new string('b', Question.MinLength));
        Difficulty newDifficulty = new(1500);
        TimeLimit newTimeLimit = new(2000);
        MemoryLimit newMemoryLimit = new(128);

        problem.UpdateContent(newTitle, newQuestion, newDifficulty, newTimeLimit, newMemoryLimit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Title, Is.EqualTo(newTitle));
            Assert.That(problem.Question, Is.EqualTo(newQuestion));
            Assert.That(problem.Difficulty, Is.EqualTo(newDifficulty));
            Assert.That(problem.TimeLimit, Is.EqualTo(newTimeLimit));
            Assert.That(problem.MemoryLimit, Is.EqualTo(newMemoryLimit));
        }
    }

    [Test]
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

        Assert.That(problem.History, Has.Count.EqualTo(1));
    }

    [Test]
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

        Assert.That(problem.History, Has.Count.EqualTo(2));
    }

    [Test]
    public void UpdateSlug_ChangesSlug()
    {
        ProblemEntity problem = CreateProblem();
        Slug newSlug = new("three-sum");

        problem.UpdateSlug(newSlug);

        Assert.That(problem.Slug, Is.EqualTo(newSlug));
    }

    [Test]
    public void AddSetup_AddsToSetups()
    {
        ProblemEntity problem = CreateProblem();

        problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());

        Assert.That(problem.Setups, Has.Count.EqualTo(1));
    }

    [Test]
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(setup.LanguageVersionId, Is.EqualTo(langVersionId));
            Assert.That(setup.InitialCode, Is.EqualTo("def twoSum():"));
            Assert.That(setup.FunctionName, Is.EqualTo("twoSum"));
        }
    }
}