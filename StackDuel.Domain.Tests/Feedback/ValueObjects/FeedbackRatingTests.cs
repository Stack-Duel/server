using StackDuel.Domain.Feedback.Exceptions;
using StackDuel.Domain.Feedback.ValueObjects;

namespace StackDuel.Domain.Tests.Feedback.ValueObjects;

public class FeedbackRatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Constructor_WithinRange_Succeeds(int value)
    {
        Assert.Null(Record.Exception(() => new FeedbackRating(value)));
    }

    [Fact]
    public void Constructor_BelowMinimum_ThrowsInvalidFeedbackRatingException()
    {
        Assert.Throws<InvalidFeedbackRatingException>(() => new FeedbackRating(0));
    }

    [Fact]
    public void Constructor_AboveMaximum_ThrowsInvalidFeedbackRatingException()
    {
        Assert.Throws<InvalidFeedbackRatingException>(() => new FeedbackRating(6));
    }

    [Fact]
    public void ImplicitOperator_ReturnsIntValue()
    {
        var rating = new FeedbackRating(4);
        int value = rating;
        Assert.Equal(4, value);
    }
}