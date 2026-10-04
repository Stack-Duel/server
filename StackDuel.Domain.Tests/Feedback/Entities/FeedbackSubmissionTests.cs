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

    [Test]
    public void Constructor_SetsStatusToNew()
    {
        var feedback = CreateFeedback();
        Assert.That(feedback.Status, Is.EqualTo(FeedbackStatus.New));
    }

    [Test]
    public void Constructor_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;
        var feedback = CreateFeedback();
        var after = DateTime.UtcNow;

        Assert.That(feedback.CreatedAt, Is.InRange(before, after));
    }

    [Test]
    public void Constructor_UpdatedAtIsNull()
    {
        var feedback = CreateFeedback();
        Assert.That(feedback.UpdatedAt, Is.Null);
    }

    [Test]
    public void UpdateStatus_SetsStatusAndAdminNoteAndUpdatedAt()
    {
        var feedback = CreateFeedback();

        feedback.UpdateStatus(FeedbackStatus.Triaged, "Looking into it.");

        Assert.Multiple(() =>
        {
            Assert.That(feedback.Status, Is.EqualTo(FeedbackStatus.Triaged));
            Assert.That(feedback.AdminNote, Is.EqualTo("Looking into it."));
            Assert.That(feedback.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public void UpdateStatus_NullAdminNote_ClearsAdminNote()
    {
        var feedback = CreateFeedback();
        feedback.UpdateStatus(FeedbackStatus.Triaged, "Initial note.");

        feedback.UpdateStatus(FeedbackStatus.Resolved, null);

        Assert.That(feedback.AdminNote, Is.Null);
    }
}