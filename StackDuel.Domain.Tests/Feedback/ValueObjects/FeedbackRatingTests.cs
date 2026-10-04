using StackDuel.Domain.Feedback.Exceptions;
using StackDuel.Domain.Feedback.ValueObjects;

namespace StackDuel.Domain.Tests.Feedback.ValueObjects;

public class FeedbackRatingTests
{
    [Test]
    public void Constructor_WithinRange_Succeeds([Values(1, 2, 3, 4, 5)] int value)
    {
        Assert.DoesNotThrow(() => new FeedbackRating(value));
    }

    [Test]
    public void Constructor_BelowMinimum_ThrowsInvalidFeedbackRatingException()
    {
        Assert.Throws<InvalidFeedbackRatingException>(() => new FeedbackRating(0));
    }

    [Test]
    public void Constructor_AboveMaximum_ThrowsInvalidFeedbackRatingException()
    {
        Assert.Throws<InvalidFeedbackRatingException>(() => new FeedbackRating(6));
    }

    [Test]
    public void ImplicitOperator_ReturnsIntValue()
    {
        var rating = new FeedbackRating(4);
        int value = rating;
        Assert.That(value, Is.EqualTo(4));
    }
}