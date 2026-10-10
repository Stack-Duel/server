using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.ValueObjects;

namespace StackDuel.Domain.Tests.Feedback.Entities;

public class FeedbackSubmissionTests
{
    private static FeedbackSubmission CreateFeedback(
        FeedbackType type = FeedbackType.General,
        FeedbackRating? rating = null,
        FeedbackContextType contextType = FeedbackContextType.None,
        Guid? contextEntityId = null
    ) =>
        new(
            Guid.NewGuid(),
            type,
            new FeedbackMessage("Something is broken."),
            rating,
            contextType,
            contextEntityId,
            "https://stackduel.dev/problems/two-sum",
            "Mozilla/5.0"
        );

    [Fact]
    public void Constructor_SetsStatusToNew()
    {
        var feedback = CreateFeedback();
        Assert.Equal(FeedbackStatus.New, feedback.Status);
    }

    [Fact]
    public void Constructor_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;
        var feedback = CreateFeedback();
        var after = DateTime.UtcNow;

        Assert.InRange(feedback.CreatedAt, before, after);
    }

    [Fact]
    public void Constructor_UpdatedAtIsNull()
    {
        var feedback = CreateFeedback();
        Assert.Null(feedback.UpdatedAt);
    }

    [Fact]
    public void UpdateStatus_SetsStatusAndAdminNoteAndUpdatedAt()
    {
        var feedback = CreateFeedback();

        feedback.UpdateStatus(FeedbackStatus.Triaged, "Looking into it.");

        Assert.Equal(FeedbackStatus.Triaged, feedback.Status);
        Assert.Equal("Looking into it.", feedback.AdminNote);
        Assert.NotNull(feedback.UpdatedAt);
    }

    [Fact]
    public void UpdateStatus_NullAdminNote_ClearsAdminNote()
    {
        var feedback = CreateFeedback();
        feedback.UpdateStatus(FeedbackStatus.Triaged, "Initial note.");

        feedback.UpdateStatus(FeedbackStatus.Resolved, null);

        Assert.Null(feedback.AdminNote);
    }
}