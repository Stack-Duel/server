using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Pagination;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Submissions;

internal sealed class SubmissionReadRepository(StackDuelDbContext context) : ISubmissionReadRepository
{
    public async Task<PageResult<ProblemSubmissionDto>> GetProblemSubmissionsPagedAsync(
        Guid problemId,
        PaginationRequest paginationRequest,
        Guid? userId,
        SubmissionFilter filter,
        SubmissionSortOrder sort,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (paginationRequest.Page - 1) * paginationRequest.Size;

        var setupIds = await context
            .Problems.AsNoTracking()
            .Include(p => p.Setups)
            .Where(p => p.Id == problemId)
            .SelectMany(p => p.Setups)
            .Select(ps => ps.Id)
            .ToListAsync(cancellationToken);

        var query = context
            .Submissions.AsNoTracking()
            .Where(s => setupIds.Contains(s.ProblemSetupId) && s.Type == SubmissionType.Submit);

        query = filter switch
        {
            SubmissionFilter.MySubmissions => query.Where(s => s.UserId == userId),
            SubmissionFilter.UserSolutions => query.Where(s => s.Status == SubmissionStatus.Accepted),
            _ => query,
        };

        int total = await query.CountAsync(cancellationToken);

        var languageVersions = context
            .Languages.AsNoTracking()
            .SelectMany(language => language.Versions, (language, version) => new { language, version });

        var orderedQuery =
            sort == SubmissionSortOrder.Oldest
                ? query.OrderBy(s => s.CreatedAt)
                : query.OrderByDescending(s => s.CreatedAt);

        var items = await orderedQuery
            .Skip(offset)
            .Take(paginationRequest.Size)
            .Join(
                context.Users.AsNoTracking(),
                submission => submission.UserId,
                user => user.Id,
                (submission, user) => new { submission, user }
            )
            .Join(
                context.Set<ProblemSetup>().AsNoTracking(),
                x => x.submission.ProblemSetupId,
                setup => setup.Id,
                (x, setup) =>
                    new
                    {
                        x.submission,
                        x.user,
                        setup,
                    }
            )
            .Join(
                languageVersions,
                x => x.setup.LanguageVersionId,
                lv => lv.version.Id,
                (x, lv) =>
                    new ProblemSubmissionDto(
                        x.submission.Id,
                        x.submission.Status,
                        new SubmissionLanguageDto(lv.language.Id, lv.language.Name.Value, lv.version.Version.Value),
                        x.submission.SourceCode.Value,
                        x.submission.CreatedAt,
                        new SubmissionUserDto(
                            x.user.Username.Value,
                            x.user.ImageUrl != null ? x.user.ImageUrl.Value : null
                        ),
                        // Submission.MemoryUsage/ExecutionTime are C#-computed properties
                        // (Results.Max(...)) wrapping a field-backed collection — not something
                        // EF's query translator can reliably turn into SQL, so they always came
                        // back null here. Aggregating over the Results navigation directly (a
                        // real mapped collection) translates to a correlated MAX subquery instead.
                        x.submission.Results.Max(r => r.MemoryUsed),
                        x.submission.Results.Max(r => r.Runtime)
                    )
            )
            .ToListAsync(cancellationToken);

        return new PageResult<ProblemSubmissionDto>
        {
            Results = items,
            Total = total,
            Page = paginationRequest.Page,
            Size = paginationRequest.Size,
        };
    }

    public async Task<PageResult<AdminSubmissionListItemDto>> GetAdminSubmissionsPagedAsync(
        PaginationRequest paginationRequest,
        Guid? id,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.Submissions.AsNoTracking();

        if (id is Guid submissionId)
            query = query.Where(s => s.Id == submissionId);

        int total = await query.CountAsync(cancellationToken);

        var ordered = query.OrderByDescending(s => s.CreatedAt);

        var page = id is null
            ? ordered.Skip((paginationRequest.Page - 1) * paginationRequest.Size).Take(paginationRequest.Size)
            : ordered;

        var items = await ProjectToAdminSubmissionListItem(page).ToListAsync(cancellationToken);

        return new PageResult<AdminSubmissionListItemDto>
        {
            Results = items,
            Total = total,
            Page = paginationRequest.Page,
            Size = paginationRequest.Size,
        };
    }

    public async Task<AdminSubmissionListItemDto?> FindAdminSubmissionByIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.Submissions.AsNoTracking().Where(s => s.Id == submissionId);

        return await ProjectToAdminSubmissionListItem(query).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfileSubmissionDto>> GetRecentSubmissionsForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var languageVersions = context
            .Languages.AsNoTracking()
            .SelectMany(language => language.Versions, (language, version) => new { language, version });

        return await context
            .Submissions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Type == SubmissionType.Submit)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .Join(
                context.Set<ProblemSetup>().AsNoTracking(),
                submission => submission.ProblemSetupId,
                setup => setup.Id,
                (submission, setup) => new { submission, setup }
            )
            .Join(
                context.Problems.AsNoTracking(),
                x => EF.Property<Guid>(x.setup, "problem_id"),
                problem => problem.Id,
                (x, problem) =>
                    new
                    {
                        x.submission,
                        x.setup,
                        problem,
                    }
            )
            .Join(
                languageVersions,
                x => x.setup.LanguageVersionId,
                lv => lv.version.Id,
                (x, lv) =>
                    new ProfileSubmissionDto(
                        x.submission.Id,
                        x.submission.Status,
                        x.problem.Title.Value,
                        x.problem.Slug.Value,
                        new SubmissionLanguageDto(lv.language.Id, lv.language.Name.Value, lv.version.Version.Value),
                        x.submission.CreatedAt
                    )
            )
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubmissionDayCountDto>> GetSubmissionCountsByDayForUserAsync(
        Guid userId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default
    )
    {
        DateTime fromDateTime = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        return await context
            .Submissions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Type == SubmissionType.Submit && s.CreatedAt >= fromDateTime)
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new SubmissionDayCountDto(DateOnly.FromDateTime(g.Key), g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAcceptedProblemIdsForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> problemIds,
        CancellationToken cancellationToken = default
    )
    {
        if (problemIds.Count == 0)
            return [];

        return await context
            .Submissions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SubmissionStatus.Accepted && s.Type == SubmissionType.Submit)
            .Join(
                context.Set<ProblemSetup>().AsNoTracking(),
                submission => submission.ProblemSetupId,
                setup => setup.Id,
                (submission, setup) => EF.Property<Guid>(setup, "problem_id")
            )
            .Where(problemId => problemIds.Contains(problemId))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<SubmissionSummaryDto?> FindSummaryAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Submissions.AsNoTracking()
            .Where(s => s.Id == submissionId)
            .Select(s => new SubmissionSummaryDto(s.UserId, s.ProblemSetupId, s.Type, s.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameSubmissionEventDto>> GetSubmissionsForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var languageVersions = context
            .Languages.AsNoTracking()
            .SelectMany(language => language.Versions, (language, version) => new { language, version });

        return await context
            .Submissions.AsNoTracking()
            .Where(s =>
                s.GameId == gameId
                && s.Type == SubmissionType.Submit
                && (s.Status == SubmissionStatus.Accepted || s.Status == SubmissionStatus.WrongAnswer)
            )
            .Join(
                context.Set<ProblemSetup>().AsNoTracking(),
                submission => submission.ProblemSetupId,
                setup => setup.Id,
                (submission, setup) => new { submission, setup }
            )
            .Join(
                context.Problems.AsNoTracking(),
                x => EF.Property<Guid>(x.setup, "problem_id"),
                problem => problem.Id,
                (x, problem) =>
                    new
                    {
                        x.submission,
                        x.setup,
                        problem,
                    }
            )
            .Join(
                languageVersions,
                x => x.setup.LanguageVersionId,
                lv => lv.version.Id,
                (x, lv) =>
                    new GameSubmissionEventDto(
                        x.submission.Id,
                        x.submission.UserId,
                        x.problem.Id,
                        x.problem.Title.Value,
                        x.problem.Slug.Value,
                        x.submission.Status,
                        x.submission.CreatedAt,
                        lv.language.Name.Value
                    )
            )
            .OrderBy(dto => dto.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AdminSubmissionListItemDto> ProjectToAdminSubmissionListItem(IQueryable<Submission> submissions)
    {
        var languageVersions = context
            .Languages.AsNoTracking()
            .SelectMany(language => language.Versions, (language, version) => new { language, version });

        return submissions
            .Join(
                context.Users.AsNoTracking(),
                submission => submission.UserId,
                user => user.Id,
                (submission, user) => new { submission, user }
            )
            .Join(
                context.Set<ProblemSetup>().AsNoTracking(),
                x => x.submission.ProblemSetupId,
                setup => setup.Id,
                (x, setup) =>
                    new
                    {
                        x.submission,
                        x.user,
                        setup,
                    }
            )
            .Join(
                context.Problems.AsNoTracking(),
                x => EF.Property<Guid>(x.setup, "problem_id"),
                problem => problem.Id,
                (x, problem) =>
                    new
                    {
                        x.submission,
                        x.user,
                        x.setup,
                        problem,
                    }
            )
            .Join(
                languageVersions,
                x => x.setup.LanguageVersionId,
                lv => lv.version.Id,
                (x, lv) =>
                    new AdminSubmissionListItemDto(
                        x.submission.Id,
                        x.submission.Type,
                        x.submission.Status,
                        x.setup.Id,
                        x.problem.Id,
                        x.problem.Title.Value,
                        x.problem.Slug.Value,
                        new SubmissionLanguageDto(lv.language.Id, lv.language.Name.Value, lv.version.Version.Value),
                        new SubmissionUserDto(
                            x.user.Username.Value,
                            x.user.ImageUrl != null ? x.user.ImageUrl.Value : null
                        ),
                        x.submission.CreatedAt,
                        x.submission.Results.Max(r => r.MemoryUsed),
                        x.submission.Results.Max(r => r.Runtime)
                    )
            );
    }
}