using StackDuel.Domain.Feedback.Exceptions;
using StackDuel.Domain.Feedback.ValueObjects;

namespace StackDuel.Domain.Tests.Feedback.ValueObjects;

public class FeedbackMessageTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string atMax = new('a', FeedbackMessage.MaxLength);
        Assert.Null(Record.Exception(() => new FeedbackMessage(atMax)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidFeedbackMessageException(string? value)
    {
        Assert.Throws<InvalidFeedbackMessageException>(() => new FeedbackMessage(value!));
    }

    [Fact]
    public void Constructor_OneAboveMaxLength_ThrowsInvalidFeedbackMessageException()
    {
        string tooLong = new('a', FeedbackMessage.MaxLength + 1);
        Assert.Throws<InvalidFeedbackMessageException>(() => new FeedbackMessage(tooLong));
    }

    [Fact]
    public void Constructor_ValidValue_SetsValue()
    {
        var message = new FeedbackMessage("The problem statement has a typo.");
        Assert.Equal("The problem statement has a typo.", message.Value);
    }

    [Fact]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var message = new FeedbackMessage("Hello world.");
        string value = message;
        Assert.Equal("Hello world.", value);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new FeedbackMessage("Hello world.");
        var b = new FeedbackMessage("Hello world.");
        Assert.Equal(b, a);
    }
}