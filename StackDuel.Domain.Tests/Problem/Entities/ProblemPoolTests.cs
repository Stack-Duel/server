using ProblemPoolEntity = StackDuel.Domain.Problems.Entities.ProblemPool;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemPoolTests
{
    private static ProblemPoolEntity CreatePool() => new("all-problems", "All Problems", "Every published problem.");

    [Fact]
    public void Constructor_NormalizesKeyToLowercase()
    {
        ProblemPoolEntity pool = new("  Game-Rotation  ", "Game Rotation");

        Assert.Equal("game-rotation", pool.Key);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        ProblemPoolEntity pool = new("featured", "  Featured  ");

        Assert.Equal("Featured", pool.Name);
    }

    [Fact]
    public void Constructor_WithNullDescription_LeavesDescriptionNull()
    {
        ProblemPoolEntity pool = new("featured", "Featured");

        Assert.Null(pool.Description);
    }

    [Fact]
    public void Constructor_WithEmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ProblemPoolEntity("   ", "Featured"));
    }

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ProblemPoolEntity("featured", "   "));
    }

    [Fact]
    public void Constructor_StartsWithNoProblems()
    {
        ProblemPoolEntity pool = CreatePool();

        Assert.Empty(pool.ProblemIds);
    }

    [Fact]
    public void AddProblem_AddsProblemId()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid problemId = Guid.NewGuid();

        pool.AddProblem(problemId);

        Assert.Contains(problemId, pool.ProblemIds);
    }

    [Fact]
    public void AddProblem_CalledTwiceWithSameId_IsIdempotent()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid problemId = Guid.NewGuid();

        pool.AddProblem(problemId);
        pool.AddProblem(problemId);

        Assert.Equal(1, pool.ProblemIds.Count(id => id == problemId));
    }

    [Fact]
    public void AddProblem_WithEmptyGuid_Throws()
    {
        ProblemPoolEntity pool = CreatePool();

        Assert.Throws<ArgumentException>(() => pool.AddProblem(Guid.Empty));
    }

    [Fact]
    public void RemoveProblem_RemovesExistingMember()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid problemId = Guid.NewGuid();
        pool.AddProblem(problemId);

        pool.RemoveProblem(problemId);

        Assert.DoesNotContain(problemId, pool.ProblemIds);
    }

    [Fact]
    public void RemoveProblem_WhenNotAMember_DoesNotThrow()
    {
        ProblemPoolEntity pool = CreatePool();

        Assert.Null(Record.Exception(() => pool.RemoveProblem(Guid.NewGuid())));
    }

    [Fact]
    public void AddProblem_AssignsIncreasingPositions()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();

        pool.AddProblem(first);
        pool.AddProblem(second);
        pool.AddProblem(third);

        Assert.Equal(new[] { first, second, third }, pool.ProblemIds);
    }

    [Fact]
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

        Assert.Equal(new[] { third, first, second }, pool.ProblemIds);
    }

    [Fact]
    public void Reorder_MissingAMember_Throws()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);

        Assert.Throws<ArgumentException>(() => pool.Reorder([first]));
    }

    [Fact]
    public void Reorder_WithUnknownId_Throws()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid member = Guid.NewGuid();
        pool.AddProblem(member);

        Assert.Throws<ArgumentException>(() => pool.Reorder([Guid.NewGuid()]));
    }

    [Fact]
    public void Reorder_WithDuplicateId_Throws()
    {
        ProblemPoolEntity pool = CreatePool();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);

        Assert.Throws<ArgumentException>(() => pool.Reorder([first, first]));
    }

    [Fact]
    public void Rename_UpdatesName()
    {
        ProblemPoolEntity pool = CreatePool();

        pool.Rename("Renamed Pool");

        Assert.Equal("Renamed Pool", pool.Name);
    }

    [Fact]
    public void UpdateDescription_WithWhitespace_ClearsDescription()
    {
        ProblemPoolEntity pool = CreatePool();

        pool.UpdateDescription("   ");

        Assert.Null(pool.Description);
    }
}