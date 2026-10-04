using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Api.Requests.Feedback;

public sealed record UpdateFeedbackStatusRequest(FeedbackStatus Status, string? AdminNote);