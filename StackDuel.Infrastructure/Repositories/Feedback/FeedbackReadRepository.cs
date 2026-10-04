using StackDuel.Application.Feedback;
using StackDuel.Application.Feedback.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Feedback;

internal sealed class FeedbackReadRepository(StackDuelDbContext context) : IFeedbackReadRepository
{
    public async Task<PageResult<AdminFeedbackListItemDto>> GetAdminFeedbackPagedAsync(
        PaginationRequest paginationRequest,
        FeedbackType? type,
        FeedbackStatus? status,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.FeedbackSubmissions.AsNoTracking();

        if (type is FeedbackType feedbackType)
            query = query.Where(f => f.Type == feedbackType);

        if (status is FeedbackStatus feedbackStatus)
            query = query.Where(f => f.Status == feedbackStatus);

        int total = await query.CountAsync(cancellationToken);

        var page = query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((paginationRequest.Page - 1) * paginationRequest.Size)
            .Take(paginationRequest.Size);

        var items = await ProjectToAdminFeedbackListItem(page).ToListAsync(cancellationToken);

        return new PageResult<AdminFeedbackListItemDto>
        {
            Results = items,
            Total = total,
            Page = paginationRequest.Page,
            Size = paginationRequest.Size,
        };
    }

    public async Task<AdminFeedbackDetailDto?> FindAdminFeedbackByIdAsync(
        Guid feedbackId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .FeedbackSubmissions.AsNoTracking()
            .Where(f => f.Id == feedbackId)
            .Join(
                context.Users.AsNoTracking(),
                feedback => feedback.UserId,
                user => user.Id,
                (feedback, user) =>
                    new AdminFeedbackDetailDto(
                        feedback.Id,
                        feedback.Type,
                        feedback.Status,
                        feedback.Message.Value,
                        feedback.Rating != null ? feedback.Rating.Value : null,
                        feedback.ContextType,
                        feedback.ContextEntityId,
                        feedback.AdminNote,
                        feedback.PageUrl,
                        feedback.UserAgent,
                        new FeedbackUserDto(user.Username.Value, user.ImageUrl != null ? user.ImageUrl.Value : null),
                        feedback.CreatedAt,
                        feedback.UpdatedAt
                    )
            )
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<AdminFeedbackListItemDto> ProjectToAdminFeedbackListItem(IQueryable<FeedbackSubmission> feedback)
    {
        return feedback.Join(
            context.Users.AsNoTracking(),
            f => f.UserId,
            user => user.Id,
            (f, user) =>
                new AdminFeedbackListItemDto(
                    f.Id,
                    f.Type,
                    f.Status,
                    f.Message.Value,
                    f.Rating != null ? f.Rating.Value : null,
                    f.ContextType,
                    f.ContextEntityId,
                    new FeedbackUserDto(user.Username.Value, user.ImageUrl != null ? user.ImageUrl.Value : null),
                    f.CreatedAt,
                    f.UpdatedAt
                )
        );
    }
}