using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.Events;
using StackDuel.Domain.Feedback.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Feedback.Entities;

public sealed class FeedbackSubmission : AggregateRoot
{
    public FeedbackSubmission(
        Guid userId,
        FeedbackType type,
        FeedbackMessage message,
        FeedbackRating? rating,
        FeedbackContextType contextType,
        Guid? contextEntityId,
        string? pageUrl,
        string? userAgent
    )
    {
        UserId = userId;
        Type = type;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Rating = rating;
        ContextType = contextType;
        ContextEntityId = contextEntityId;
        PageUrl = pageUrl;
        UserAgent = userAgent;
        Status = FeedbackStatus.New;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new FeedbackSubmittedDomainEvent(Id, UserId, Type));
    }

    public void UpdateStatus(FeedbackStatus status, string? adminNote)
    {
        Status = status;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }

    private FeedbackSubmission() { }

    public Guid UserId { get; private set; }
    public FeedbackType Type { get; private set; }
    public FeedbackMessage Message { get; private set; } = null!;
    public FeedbackRating? Rating { get; private set; }
    public FeedbackContextType ContextType { get; private set; }
    public Guid? ContextEntityId { get; private set; }
    public FeedbackStatus Status { get; private set; }
    public string? AdminNote { get; private set; }
    public string? PageUrl { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}