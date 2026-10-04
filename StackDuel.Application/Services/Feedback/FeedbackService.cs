using StackDuel.Application.Commands.Feedback.SubmitFeedback;
using StackDuel.Application.Commands.Feedback.UpdateFeedbackStatus;
using StackDuel.Application.Feedback.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Feedback.GetAdminFeedbackDetail;
using StackDuel.Application.Queries.Feedback.GetAdminFeedbackPageable;
using StackDuel.Domain.Feedback.Enums;
using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Services.Feedback;

public interface IFeedbackService
{
    Task<Result<Guid>> SubmitFeedbackAsync(SubmitFeedbackDto dto, CancellationToken cancellationToken);

    Task<Result> UpdateFeedbackStatusAsync(
        Guid feedbackId,
        FeedbackStatus status,
        string? adminNote,
        CancellationToken cancellationToken
    );

    Task<Result<PageResult<AdminFeedbackListItemDto>>> GetAdminFeedbackPageableAsync(
        PaginationRequest paginationRequest,
        FeedbackType? type,
        FeedbackStatus? status,
        CancellationToken cancellationToken
    );

    Task<Result<AdminFeedbackDetailDto>> GetAdminFeedbackDetailAsync(
        Guid feedbackId,
        CancellationToken cancellationToken
    );
}

internal sealed class FeedbackService(IMediator mediator) : IFeedbackService
{
    public async Task<Result<Guid>> SubmitFeedbackAsync(SubmitFeedbackDto dto, CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new SubmitFeedbackCommand(
                dto.UserId,
                dto.Type,
                dto.Message,
                dto.Rating,
                dto.ContextType,
                dto.ContextEntityId,
                dto.PageUrl,
                dto.UserAgent
            ),
            cancellationToken
        );
    }

    public async Task<Result> UpdateFeedbackStatusAsync(
        Guid feedbackId,
        FeedbackStatus status,
        string? adminNote,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new UpdateFeedbackStatusCommand(feedbackId, status, adminNote), cancellationToken);
    }

    public async Task<Result<PageResult<AdminFeedbackListItemDto>>> GetAdminFeedbackPageableAsync(
        PaginationRequest paginationRequest,
        FeedbackType? type,
        FeedbackStatus? status,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new GetAdminFeedbackPageableQuery(paginationRequest, type, status),
            cancellationToken
        );
    }

    public async Task<Result<AdminFeedbackDetailDto>> GetAdminFeedbackDetailAsync(
        Guid feedbackId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetAdminFeedbackDetailQuery(feedbackId), cancellationToken);
    }
}