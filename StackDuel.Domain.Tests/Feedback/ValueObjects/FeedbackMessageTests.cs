using StackDuel.Domain.Feedback.Exceptions;
using StackDuel.Domain.Feedback.ValueObjects;

namespace StackDuel.Domain.Tests.Feedback.ValueObjects;

public class FeedbackMessageTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string atMax = new('a', FeedbackMessage.MaxLength);
        Assert.DoesNotThrow(() => new FeedbackMessage(atMax));
    }

    [Test]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidFeedbackMessageException(
        [Values("", " ", "   ", null)] string? value
    )
    {
        Assert.Throws<InvalidFeedbackMessageException>(() => new FeedbackMessage(value!));
    }

    [Test]
    public void Constructor_OneAboveMaxLength_ThrowsInvalidFeedbackMessageException()
    {
        string tooLong = new('a', FeedbackMessage.MaxLength + 1);
        Assert.Throws<InvalidFeedbackMessageException>(() => new FeedbackMessage(tooLong));
    }

    [Test]
    public void Constructor_ValidValue_SetsValue()
    {
        var message = new FeedbackMessage("The problem statement has a typo.");
        Assert.That(message.Value, Is.EqualTo("The problem statement has a typo."));
    }

    [Test]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var message = new FeedbackMessage("Hello world.");
        string value = message;
        Assert.That(value, Is.EqualTo("Hello world."));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new FeedbackMessage("Hello world.");
        var b = new FeedbackMessage("Hello world.");
        Assert.That(a, Is.EqualTo(b));
    }
}