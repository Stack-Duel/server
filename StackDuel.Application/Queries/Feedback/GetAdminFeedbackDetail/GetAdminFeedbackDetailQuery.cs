using StackDuel.Application.Feedback.Dtos;

namespace StackDuel.Application.Queries.Feedback.GetAdminFeedbackDetail;

public sealed record GetAdminFeedbackDetailQuery(Guid FeedbackId) : IQuery<AdminFeedbackDetailDto>;