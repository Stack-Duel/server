using StackDuel.Domain.Submissions.Exceptions;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Tests.Submissions.ValueObjects;

public class SourceCodeTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', SourceCode.MaxLength);

        Assert.Null(Record.Exception(() => new SourceCode(value)));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidSourceCodeException()
    {
        Assert.Throws<InvalidSourceCodeException>(() => new SourceCode(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidSourceCodeException()
    {
        string value = new('a', SourceCode.MaxLength + 1);

        Assert.Throws<InvalidSourceCodeException>(() => new SourceCode(value));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidSourceCodeException()
    {
        Assert.Throws<InvalidSourceCodeException>(() => new SourceCode("   "));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new SourceCode("int main() {}");
        var b = new SourceCode("def solve(): pass");

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new SourceCode("int main() {}");
        var b = new SourceCode("int main() {}");

        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var code = new SourceCode("int main() {}");

        string result = code;

        Assert.Equal("int main() {}", result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var code = new SourceCode("int main() {}");

        Assert.Equal("int main() {}", code.ToString());
    }
}