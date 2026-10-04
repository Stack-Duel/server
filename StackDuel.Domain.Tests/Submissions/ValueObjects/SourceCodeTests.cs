using StackDuel.Domain.Submissions.Exceptions;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Tests.Submissions.ValueObjects;

public class SourceCodeTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', SourceCode.MaxLength);

        Assert.That(() => new SourceCode(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidSourceCodeException()
    {
        Assert.Throws<InvalidSourceCodeException>(() => new SourceCode(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidSourceCodeException()
    {
        string value = new('a', SourceCode.MaxLength + 1);

        Assert.Throws<InvalidSourceCodeException>(() => new SourceCode(value));
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidSourceCodeException()
    {
        Assert.Throws<InvalidSourceCodeException>(() => new SourceCode("   "));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new SourceCode("int main() {}");
        var b = new SourceCode("def solve(): pass");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new SourceCode("int main() {}");
        var b = new SourceCode("int main() {}");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var code = new SourceCode("int main() {}");

        string result = code;

        Assert.That(result, Is.EqualTo("int main() {}"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var code = new SourceCode("int main() {}");

        Assert.That(code.ToString(), Is.EqualTo("int main() {}"));
    }
}