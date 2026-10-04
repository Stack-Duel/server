using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Feedback.Events;

public sealed record FeedbackSubmittedDomainEvent(Guid FeedbackSubmissionId, Guid UserId, FeedbackType Type)
    : IDomainEvent;