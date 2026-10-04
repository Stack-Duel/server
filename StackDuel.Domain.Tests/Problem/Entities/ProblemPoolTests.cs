using ProblemPoolEntity = StackDuel.Domain.Problems.Entities.ProblemPool;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemPoolTests
{
    private static ProblemPoolEntity CreatePool() => new("all-problems", "All Problems", "Every published problem.");

    [Test]
    public void Constructor_NormalizesKeyToLowercase()
    {
        ProblemPoolEntity pool = new("  Game-Rotation  ", "Game Rotation");

        Assert.That(pool.Key, Is.EqualTo("game-rotation"));
    }

    [Test]
    public void Constructor_TrimsName()
    {
        ProblemPoolEntity pool = new("featured", "  Featured  ");

        Assert.That(pool.Name, Is.EqualTo("Featured"));
    }

    [Test]
    public void Constructor_WithNullDescription_LeavesDescriptionNull()
    {
        ProblemPoolEntity pool = new("featured", "Featured");

        Assert.That(pool.Description, Is.Null);
    }

    [Test]
    public void Constructor_WithEmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ProblemPoolEntity("   ", "Featured"));
    }

    [Test]
    public void Constructor_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ProblemPoolEntity("featured", "   "));
    }

    [Test]
    public void Constructor_StartsWithNoProblems()
    {
        ProblemPoolEntity pool = CreatePool();

        Assert.That(pool.ProblemIds, Is.Empty);
    }

    [Test]
    public void AddProblem_AddsProblemId()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid problemId = Guid.NewGuid();

        pool.AddProblem(problemId);

        Assert.That(pool.ProblemIds, Does.Contain(problemId));
    }

    [Test]
    public void AddProblem_CalledTwiceWithSameId_IsIdempotent()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid problemId = Guid.NewGuid();

        pool.AddProblem(problemId);
        pool.AddProblem(problemId);

        Assert.That(pool.ProblemIds.Count(id => id == problemId), Is.EqualTo(1));
    }

    [Test]
    public void AddProblem_WithEmptyGuid_Throws()
    {
        ProblemPoolEntity pool = CreatePool();

        Assert.Throws<ArgumentException>(() => pool.AddProblem(Guid.Empty));
    }

    [Test]
    public void RemoveProblem_RemovesExistingMember()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid problemId = Guid.NewGuid();
        pool.AddProblem(problemId);

        pool.RemoveProblem(problemId);

        Assert.That(pool.ProblemIds, Does.Not.Contain(problemId));
    }

    [Test]
    public void RemoveProblem_WhenNotAMember_DoesNotThrow()
    {
        ProblemPoolEntity pool = CreatePool();

        Assert.DoesNotThrow(() => pool.RemoveProblem(Guid.NewGuid()));
    }

    [Test]
    public void AddProblem_AssignsIncreasingPositions()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();

        pool.AddProblem(first);
        pool.AddProblem(second);
        pool.AddProblem(third);

        Assert.That(pool.ProblemIds, Is.EqualTo(new[] { first, second, third }));
    }

    [Test]
    public void Reorder_ReassignsPositionsToMatchGivenOrder()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);
        pool.AddProblem(third);

        pool.Reorder([third, first, second]);

        Assert.That(pool.ProblemIds, Is.EqualTo(new[] { third, first, second }));
    }

    [Test]
    public void Reorder_MissingAMember_Throws()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);

        Assert.Throws<ArgumentException>(() => pool.Reorder([first]));
    }

    [Test]
    public void Reorder_WithUnknownId_Throws()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid member = Guid.NewGuid();
        pool.AddProblem(member);

        Assert.Throws<ArgumentException>(() => pool.Reorder([Guid.NewGuid()]));
    }

    [Test]
    public void Reorder_WithDuplicateId_Throws()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);

        Assert.Throws<ArgumentException>(() => pool.Reorder([first, first]));
    }

    [Test]
    public void Rename_UpdatesName()
    {
        ProblemPoolEntity pool = CreatePool();

        pool.Rename("Renamed Pool");

        Assert.That(pool.Name, Is.EqualTo("Renamed Pool"));
    }

    [Test]
    public void UpdateDescription_WithWhitespace_ClearsDescription()
    {
        ProblemPoolEntity pool = CreatePool();

        pool.UpdateDescription("   ");

        Assert.That(pool.Description, Is.Null);
    }
}