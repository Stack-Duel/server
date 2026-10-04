using Ardalis.Result;
using StackDuel.Application.Feedback;
using StackDuel.Application.Feedback.Dtos;

namespace StackDuel.Application.Queries.Feedback.GetAdminFeedbackDetail;

internal sealed class GetAdminFeedbackDetailHandler(IFeedbackReadRepository feedbackReadRepository)
    : IQueryHandler<GetAdminFeedbackDetailQuery, AdminFeedbackDetailDto>
{
    public async Task<Result<AdminFeedbackDetailDto>> Handle(
        GetAdminFeedbackDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var feedback = await feedbackReadRepository.FindAdminFeedbackByIdAsync(request.FeedbackId, cancellationToken);

        if (feedback is null)
            return Result<AdminFeedbackDetailDto>.NotFound();

        return Result.Success(feedback);
    }
}