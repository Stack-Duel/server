using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Submissions.CreateSubmission;
using StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Submissions.GetAdminSubmissionDetail;
using StackDuel.Application.Queries.Submissions.GetAdminSubmissionsPageable;
using StackDuel.Application.Queries.Submissions.GetSubmissionsByProblemSlug;
using StackDuel.Application.Queries.Submissions.GetSubmissionStatus;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Services.Submissions;

public interface ISubmissionService
{
    Task<Result<Guid>> CreateSubmissionAsync(CreateSubmissionDto dto, CancellationToken cancellationToken);

    Task<Result<SubmissionStatusDto>> GetSubmissionStatusAsync(
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<PageResult<ProblemSubmissionDto>>> GetSubmissionsByProblemSlugAsync(
        string slug,
        PaginationRequest paginationRequest,
        Guid? userId,
        SubmissionFilter type,
        SubmissionSortOrder sortOrder,
        CancellationToken cancellationToken
    );

    Task<Result<PageResult<AdminSubmissionListItemDto>>> GetAdminSubmissionsPageableAsync(
        PaginationRequest paginationRequest,
        Guid? id,
        CancellationToken cancellationToken
    );

    Task<Result<AdminSubmissionDetailDto>> GetAdminSubmissionDetailAsync(
        Guid submissionId,
        CancellationToken cancellationToken
    );

    Task<Result> ReceiveJudge0CallbackAsync(string? key, Guid submissionId, CancellationToken cancellationToken);
}

internal sealed class SubmissionService(IMediator mediator) : ISubmissionService
{
    public async Task<Result<Guid>> CreateSubmissionAsync(CreateSubmissionDto dto, CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new CreateSubmissionCommand(
                dto.ProblemSetupId,
                dto.Type,
                dto.Code,
                dto.CreatedById,
                dto.CustomTestCases,
                dto.AdditionalFiles
            ),
            cancellationToken
        );
    }

    public async Task<Result<PageResult<ProblemSubmissionDto>>> GetSubmissionsByProblemSlugAsync(
        string slug,
        PaginationRequest paginationRequest,
        Guid? userId,
        SubmissionFilter type,
        SubmissionSortOrder sortOrder,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new GetSubmissionsByProblemSlugQuery(slug, paginationRequest, userId, type, sortOrder),
            cancellationToken
        );
    }

    public async Task<Result<SubmissionStatusDto>> GetSubmissionStatusAsync(
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetSubmissionStatusQuery(submissionId, userId), cancellationToken);
    }

    public async Task<Result<PageResult<AdminSubmissionListItemDto>>> GetAdminSubmissionsPageableAsync(
        PaginationRequest paginationRequest,
        Guid? id,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetAdminSubmissionsPageableQuery(paginationRequest, id), cancellationToken);
    }

    public async Task<Result<AdminSubmissionDetailDto>> GetAdminSubmissionDetailAsync(
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetAdminSubmissionDetailQuery(submissionId), cancellationToken);
    }

    public async Task<Result> ReceiveJudge0CallbackAsync(
        string? key,
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new ReceiveJudge0CallbackCommand(key, submissionId), cancellationToken);
    }
}