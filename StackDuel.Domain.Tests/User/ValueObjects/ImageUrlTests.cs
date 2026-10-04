using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Tests.User.ValueObjects;

public class ImageUrlTests
{
    private const string ValidHttpUrl = "https://example.com/avatar.png";

    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string path = new('a', ImageUrl.MaxLength - "https://x.co/".Length);
        string atMax = $"https://x.co/{path}";
        Assert.DoesNotThrow(() => new ImageUrl(atMax));
    }

    [Test]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidImageUrlException(
        [Values("", " ", "   ", null)] string? value
    )
    {
        Assert.Throws<InvalidImageUrlException>(() => new ImageUrl(value!));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidImageUrlException()
    {
        string path = new('a', ImageUrl.MaxLength);
        string tooLong = $"https://x.co/{path}";
        Assert.Throws<InvalidImageUrlException>(() => new ImageUrl(tooLong));
    }

    [Test]
    public void Constructor_InvalidUrl_ThrowsInvalidImageUrlException(
        [Values("not-a-url", "ftp://example.com/file.png", "example.com/avatar.png", "//example.com/avatar.png")]
            string value
    )
    {
        Assert.Throws<InvalidImageUrlException>(() => new ImageUrl(value));
    }

    [Test]
    public void Constructor_ValidUrl_SetsValue(
        [Values(
            "https://example.com/avatar.png",
            "http://example.com/avatar.jpg",
            "https://avatars.githubusercontent.com/u/12345"
        )]
            string value
    )
    {
        var imageUrl = new ImageUrl(value);
        Assert.That(imageUrl.Value, Is.EqualTo(value));
    }

    [Test]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = new ImageUrl(ValidHttpUrl);
        var b = new ImageUrl("https://example.com/other.png");
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new ImageUrl(ValidHttpUrl);
        var b = new ImageUrl(ValidHttpUrl);
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var imageUrl = new ImageUrl(ValidHttpUrl);
        string value = imageUrl;
        Assert.That(value, Is.EqualTo(ValidHttpUrl));
    }

    [Test]
    public void ToString_MatchesImplicitOperator()
    {
        var imageUrl = new ImageUrl(ValidHttpUrl);
        Assert.That(imageUrl.ToString(), Is.EqualTo((string)imageUrl));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var imageUrl = new ImageUrl(ValidHttpUrl);
        Assert.That(imageUrl.ToString(), Is.EqualTo(ValidHttpUrl));
    }
}