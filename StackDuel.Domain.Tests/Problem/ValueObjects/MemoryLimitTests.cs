using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class MemoryLimitTests
{
    [Test]
    public void Constructor_AtMaxMegabytes_Succeeds()
    {
        Assert.That(() => new MemoryLimit(MemoryLimit.MaxMegabytes), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinMegabytes_Succeeds()
    {
        Assert.That(() => new MemoryLimit(MemoryLimit.MinMegabytes), Throws.Nothing);
    }

    [Test]
    public void Constructor_AboveMaxMegabytes_ThrowsInvalidMemoryLimitException()
    {
        Assert.Throws<InvalidMemoryLimitException>(() => new MemoryLimit(MemoryLimit.MaxMegabytes + 1));
    }

    [Test]
    public void Constructor_BelowMinMegabytes_ThrowsInvalidMemoryLimitException()
    {
        Assert.Throws<InvalidMemoryLimitException>(() => new MemoryLimit(MemoryLimit.MinMegabytes - 1));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new MemoryLimit(64);
        var b = new MemoryLimit(128);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new MemoryLimit(64);
        var b = new MemoryLimit(64);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ToString_IncludesMegabytes()
    {
        var memoryLimit = new MemoryLimit(64);

        Assert.That(memoryLimit.ToString(), Is.EqualTo("64MB"));
    }
}