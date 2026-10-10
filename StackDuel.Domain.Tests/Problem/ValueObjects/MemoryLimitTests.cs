using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class MemoryLimitTests
{
    [Fact]
    public void Constructor_AtMaxMegabytes_Succeeds()
    {
        Assert.Null(Record.Exception(() => new MemoryLimit(MemoryLimit.MaxMegabytes)));
    }

    [Fact]
    public void Constructor_AtMinMegabytes_Succeeds()
    {
        Assert.Null(Record.Exception(() => new MemoryLimit(MemoryLimit.MinMegabytes)));
    }

    [Fact]
    public void Constructor_AboveMaxMegabytes_ThrowsInvalidMemoryLimitException()
    {
        Assert.Throws<InvalidMemoryLimitException>(() => new MemoryLimit(MemoryLimit.MaxMegabytes + 1));
    }

    [Fact]
    public void Constructor_BelowMinMegabytes_ThrowsInvalidMemoryLimitException()
    {
        Assert.Throws<InvalidMemoryLimitException>(() => new MemoryLimit(MemoryLimit.MinMegabytes - 1));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new MemoryLimit(64);
        var b = new MemoryLimit(128);

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new MemoryLimit(64);
        var b = new MemoryLimit(64);

        Assert.Equal(b, a);
    }

    [Fact]
    public void ToString_IncludesMegabytes()
    {
        var memoryLimit = new MemoryLimit(64);

        Assert.Equal("64MB", memoryLimit.ToString());
    }
}