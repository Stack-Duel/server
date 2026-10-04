using StackDuel.Application.Pagination;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Submissions;

public interface ISubmissionReadRepository
{
    Task<PageResult<ProblemSubmissionDto>> GetProblemSubmissionsPagedAsync(
        Guid problemId,
        PaginationRequest paginationRequest,
        Guid? userId,
        SubmissionFilter filter,
        SubmissionSortOrder sort,
        CancellationToken cancellationToken = default
    );

    Task<PageResult<AdminSubmissionListItemDto>> GetAdminSubmissionsPagedAsync(
        PaginationRequest paginationRequest,
        Guid? id,
        CancellationToken cancellationToken = default
    );

    Task<AdminSubmissionListItemDto?> FindAdminSubmissionByIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// A user's most recent "Submit"-type submissions (never "Run"/test submissions), newest
    /// first — backs the profile's recent-submissions list. Metadata only, source code is
    /// deliberately never projected here: this is shown to other visitors, not just the owner.
    /// </summary>
    Task<IReadOnlyList<ProfileSubmissionDto>> GetRecentSubmissionsForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SubmissionDayCountDto>> GetSubmissionCountsByDayForUserAsync(
        Guid userId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default
    );

    Task<SubmissionSummaryDto?> FindSummaryAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every terminal (Accepted/WrongAnswer) "Submit"-type submission made in a game, across all
    /// participants, oldest first — the raw material for the admin game history timeline. Queued
    /// and Running submissions are excluded since they're transient in-flight state, not a
    /// meaningful past event.
    /// </summary>
    Task<IReadOnlyList<GameSubmissionEventDto>> GetSubmissionsForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Which of the given problem ids the user has an accepted "Submit"-type submission for —
    /// used to determine which daily challenges a user has solved.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAcceptedProblemIdsForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> problemIds,
        CancellationToken cancellationToken = default
    );
}