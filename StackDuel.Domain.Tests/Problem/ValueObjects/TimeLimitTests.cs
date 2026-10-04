using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class TimeLimitTests
{
    [Test]
    public void Constructor_AtMaxMilliseconds_Succeeds()
    {
        Assert.That(() => new TimeLimit(TimeLimit.MaxMilliseconds), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinMilliseconds_Succeeds()
    {
        Assert.That(() => new TimeLimit(TimeLimit.MinMilliseconds), Throws.Nothing);
    }

    [Test]
    public void Constructor_AboveMaxMilliseconds_ThrowsInvalidTimeLimitException()
    {
        Assert.Throws<InvalidTimeLimitException>(() => new TimeLimit(TimeLimit.MaxMilliseconds + 1));
    }

    [Test]
    public void Constructor_BelowMinMilliseconds_ThrowsInvalidTimeLimitException()
    {
        Assert.Throws<InvalidTimeLimitException>(() => new TimeLimit(TimeLimit.MinMilliseconds - 1));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new TimeLimit(1000);
        var b = new TimeLimit(2000);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new TimeLimit(1000);
        var b = new TimeLimit(1000);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ToString_IncludesMilliseconds()
    {
        var timeLimit = new TimeLimit(1000);

        Assert.That(timeLimit.ToString(), Is.EqualTo("1000ms"));
    }
}