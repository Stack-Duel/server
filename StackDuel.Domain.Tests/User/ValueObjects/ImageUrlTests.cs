using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Tests.User.ValueObjects;

public class ImageUrlTests
{
    private const string ValidHttpUrl = "https://example.com/avatar.png";

    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string path = new('a', ImageUrl.MaxLength - "https://x.co/".Length);
        string atMax = $"https://x.co/{path}";
        Assert.Null(Record.Exception(() => new ImageUrl(atMax)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidImageUrlException(string? value)
    {
        Assert.Throws<InvalidImageUrlException>(() => new ImageUrl(value!));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidImageUrlException()
    {
        string path = new('a', ImageUrl.MaxLength);
        string tooLong = $"https://x.co/{path}";
        Assert.Throws<InvalidImageUrlException>(() => new ImageUrl(tooLong));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file.png")]
    [InlineData("example.com/avatar.png")]
    [InlineData("//example.com/avatar.png")]
    public void Constructor_InvalidUrl_ThrowsInvalidImageUrlException(string value)
    {
        Assert.Throws<InvalidImageUrlException>(() => new ImageUrl(value));
    }

    [Theory]
    [InlineData("https://example.com/avatar.png")]
    [InlineData("http://example.com/avatar.jpg")]
    [InlineData("https://avatars.githubusercontent.com/u/12345")]
    public void Constructor_ValidUrl_SetsValue(string value)
    {
        var imageUrl = new ImageUrl(value);
        Assert.Equal(value, imageUrl.Value);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = new ImageUrl(ValidHttpUrl);
        var b = new ImageUrl("https://example.com/other.png");
        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new ImageUrl(ValidHttpUrl);
        var b = new ImageUrl(ValidHttpUrl);
        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var imageUrl = new ImageUrl(ValidHttpUrl);
        string value = imageUrl;
        Assert.Equal(ValidHttpUrl, value);
    }

    [Fact]
    public void ToString_MatchesImplicitOperator()
    {
        var imageUrl = new ImageUrl(ValidHttpUrl);
        Assert.Equal((string)imageUrl, imageUrl.ToString());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var imageUrl = new ImageUrl(ValidHttpUrl);
        Assert.Equal(ValidHttpUrl, imageUrl.ToString());
    }
}