using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class TimeLimitTests
{
    [Fact]
    public void Constructor_AtMaxMilliseconds_Succeeds()
    {
        Assert.Null(Record.Exception(() => new TimeLimit(TimeLimit.MaxMilliseconds)));
    }

    [Fact]
    public void Constructor_AtMinMilliseconds_Succeeds()
    {
        Assert.Null(Record.Exception(() => new TimeLimit(TimeLimit.MinMilliseconds)));
    }

    [Fact]
    public void Constructor_AboveMaxMilliseconds_ThrowsInvalidTimeLimitException()
    {
        Assert.Throws<InvalidTimeLimitException>(() => new TimeLimit(TimeLimit.MaxMilliseconds + 1));
    }

    [Fact]
    public void Constructor_BelowMinMilliseconds_ThrowsInvalidTimeLimitException()
    {
        Assert.Throws<InvalidTimeLimitException>(() => new TimeLimit(TimeLimit.MinMilliseconds - 1));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new TimeLimit(1000);
        var b = new TimeLimit(2000);

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new TimeLimit(1000);
        var b = new TimeLimit(1000);

        Assert.Equal(b, a);
    }

    [Fact]
    public void ToString_IncludesMilliseconds()
    {
        var timeLimit = new TimeLimit(1000);

        Assert.Equal("1000ms", timeLimit.ToString());
    }
}