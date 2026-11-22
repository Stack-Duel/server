using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemReadRepository(StackDuelDbContext context) : IProblemReadRepository
{
    private static readonly List<ProblemStatus> ResolvedStatuses = [.. Problem.ResolveSubmittedStatuses];

    private static IQueryable<Problem> ApplyAdminSearchFilter(IQueryable<Problem> query, string? search)
    {
        string? term = search?.Trim();
        if (string.IsNullOrEmpty(term))
            return query;

        if (Guid.TryParse(term, out Guid id))
            return query.Where(p => p.Id == id);

        string pattern = $"%{term}%";
        return query.Where(p =>
            EF.Functions.ILike(p.Title.Value, pattern) || EF.Functions.ILike(p.Slug.Value, pattern)
        );
    }

    public async Task<int> CountUnresolvedProblemsByCreatorAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .AsNoTracking()
            .Where(problem => problem.CreatedById == creatorId)
            .Where(problem => !ResolvedStatuses.Contains(problem.Status))
            .CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Problems.AsNoTracking().AnyAsync(problem => problem.Id == id, cancellationToken);

    public async Task<bool> ExistsForAdminAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .AsNoTracking()
            .AnyAsync(problem => problem.Id == id, cancellationToken);

    public async Task<Problem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context
            .Problems.AsNoTracking()
            .AsSplitQuery()
            .Include(problem => problem.CreatedBy)
            .Include(problem => problem.Tags)
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.Inputs.OrderBy(i => i.Position))
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.ExpectedOutputs)
            .Where(problem => problem.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Problem?> FindByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .AsNoTracking()
            .AsSplitQuery()
            .Include(problem => problem.CreatedBy)
            .Include(problem => problem.Tags)
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.Inputs.OrderBy(i => i.Position))
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.ExpectedOutputs)
            .Where(problem => problem.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminProblemListRowDto>> FindByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default
    )
    {
        if (ids.Count == 0)
            return [];

        return await context
            .Problems.IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .OrderBy(p => p.Title.Value)
            .Select(p => new AdminProblemListRowDto(
                p.Id,
                p.Slug.Value,
                p.Title.Value,
                p.Difficulty.Value,
                p.Status,
                p.TimeLimit.Milliseconds,
                p.MemoryLimit.Megabytes,
                p.Tags.Select(t => t.Name.Value).ToList(),
                p.Setups.Select(s => s.LanguageVersionId).Distinct().ToList(),
                p.Setups.Count,
                p.CreatedAt,
                p.CreatedBy != null ? p.CreatedBy.Username.Value : null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<Problem?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await context
            .Problems.AsNoTracking()
            .AsSplitQuery()
            .Include(problem => problem.CreatedBy)
            .Include(problem => problem.Tags)
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.Inputs.OrderBy(i => i.Position))
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.ExpectedOutputs)
            .Include(problem => problem.Versions)
                .ThenInclude(version => version.Setups)
            .Where(problem => problem.Slug.Value == slug)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProblemDifficultyLookupDto?> FindDifficultyByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(setup => setup.Id == problemSetupId)
            .Join(
                context.Problems.AsNoTracking().IgnoreQueryFilters(),
                setup => EF.Property<Guid>(setup, "problem_id"),
                problem => problem.Id,
                (setup, problem) => new ProblemDifficultyLookupDto(problem.Id, problem.Difficulty.Value)
            )
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> FindPublishedVersionIdAsync(Guid problemId, CancellationToken cancellationToken = default)
    {
        return await context
            .Problems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(problem => problem.Id == problemId)
            .Select(problem => problem.PublishedVersionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> FindPublishedVersionIdByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(setup => setup.Id == problemSetupId)
            .Join(
                context.Problems.AsNoTracking().IgnoreQueryFilters(),
                setup => EF.Property<Guid>(setup, "problem_id"),
                problem => problem.Id,
                (setup, problem) => problem.PublishedVersionId
            )
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StudioProblemDetailsDto?> FindStudioProblemDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .Problems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .Where(p => p.Id == id)
            .Select(p => new StudioProblemDetailsDto(
                p.Id,
                p.Slug.Value,
                p.Title.Value,
                p.Difficulty.Value,
                p.Status,
                p.TimeLimit.Milliseconds,
                p.MemoryLimit.Megabytes,
                p.Setups.Count,
                p.CreatedAt,
                p.Question,
                0,
                0,
                0,
                DifficultyTier.Beginner,
                null,
                p.TrackId,
                p.Tags.Select(tag => tag.Name.Value).ToList(),
                p.GenerationSpec == null ? null : p.GenerationSpec.Parameters,
                p.GenerationSpec == null ? null : p.GenerationSpec.OutputValueType,
                p.GenerationSpec == null ? null : p.GenerationSpec.TargetCaseCount,
                p.GenerationSpec == null ? null : p.GenerationSpec.Seed,
                p.HasUnpublishedChanges,
                p.IsRevalidating,
                p.PublishedVersionId != null
            ))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ProblemPublishReadinessProjection?> FindPublishReadinessAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .Problems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .Where(p => p.Id == id)
            .Select(p => new ProblemPublishReadinessProjection(
                p.Setups.Count,
                p.Setups.Where(setup => setup.ReferenceSolutionCode == null || setup.ReferenceSolutionCode == "")
                    .Select(setup => setup.LanguageVersionId)
                    .ToList(),
                p.Setups.SelectMany(setup => setup.TestSuites)
                    .Where(testSuite => testSuite.Type == TestSuiteType.Sample)
                    .SelectMany(testSuite => testSuite.TestCases)
                    .Count(testCase => testCase.Source == TestCaseSource.Authored && testCase.RetiredAt == null),
                p.GenerationSpec != null
            ))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetAdminProblemIdsMatchingAsync(
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyAdminSearchFilter(
            context.Problems.IgnoreQueryFilters().AsNoTracking().Where(p => p.DeletedAt == null).AsQueryable(),
            search
        );

        return await query.Select(p => p.Id).ToListAsync(cancellationToken);
    }

    public async Task<PageResult<AdminProblemListRowDto>> GetAdminProblemsPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        var query = ApplyAdminSearchFilter(
            context.Problems.IgnoreQueryFilters().AsNoTracking().Where(p => p.DeletedAt == null).AsQueryable(),
            search
        );

        int total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(offset)
            .Take(pagination.Size)
            .Select(p => new AdminProblemListRowDto(
                p.Id,
                p.Slug.Value,
                p.Title.Value,
                p.Difficulty.Value,
                p.Status,
                p.TimeLimit.Milliseconds,
                p.MemoryLimit.Megabytes,
                p.Tags.Select(t => t.Name.Value).ToList(),
                p.Setups.Select(s => s.LanguageVersionId).Distinct().ToList(),
                p.Setups.Count,
                p.CreatedAt,
                p.CreatedBy != null ? p.CreatedBy.Username.Value : null
            ))
            .ToListAsync(cancellationToken);

        return new PageResult<AdminProblemListRowDto>
        {
            Results = items,
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }

    public async Task<Guid?> GetCreatedByIdAsync(Guid problemId, CancellationToken cancellationToken = default) =>
        await context
            .Problems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.Id == problemId)
            .Select(p => p.CreatedById)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await context
            .Problems.AsNoTracking()
            .Where(p => p.Slug.Value == slug)
            .Select(p => p.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PageResult<AdminProblemListRowDto>> GetMyProblemsPagedAsync(
        Guid creatorId,
        IReadOnlyCollection<ProblemStatus> statuses,
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        List<ProblemStatus> statusList = [.. statuses];

        var query = ApplyAdminSearchFilter(
            context
                .Problems.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.CreatedById == creatorId && p.DeletedAt == null),
            search
        );

        if (statusList.Count > 0)
            query = query.Where(p => statusList.Contains(p.Status));

        int total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(offset)
            .Take(pagination.Size)
            .Select(p => new AdminProblemListRowDto(
                p.Id,
                p.Slug.Value,
                p.Title.Value,
                p.Difficulty.Value,
                p.Status,
                p.TimeLimit.Milliseconds,
                p.MemoryLimit.Megabytes,
                p.Tags.Select(t => t.Name.Value).ToList(),
                p.Setups.Select(s => s.LanguageVersionId).Distinct().ToList(),
                p.Setups.Count,
                p.CreatedAt,
                p.CreatedBy != null ? p.CreatedBy.Username.Value : null
            ))
            .ToListAsync(cancellationToken);

        return new PageResult<AdminProblemListRowDto>
        {
            Results = items,
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }

    public async Task<IReadOnlyList<Guid>> GetOrderedEligibleProblemIdsAsync(
        Guid poolId,
        CancellationToken cancellationToken = default
    )
    {
        var poolItems = context.Set<ProblemPoolItem>().Where(item => EF.Property<Guid>(item, "pool_id") == poolId);

        return await poolItems
            .Join(
                context.Problems.Where(problem => problem.Status == ProblemStatus.Published),
                item => item.ProblemId,
                problem => problem.Id,
                (item, problem) => new { item.Position, problem.Id }
            )
            .OrderBy(x => x.Position)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PageResult<ProblemListRowDto>> GetPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        var query =
            from problem in context.Problems.AsNoTracking()
            join version in context.Set<ProblemVersion>().AsNoTracking()
                on problem.PublishedVersionId equals (Guid?)version.Id
                into publishedVersions
            from publishedVersion in publishedVersions.DefaultIfEmpty()
            where problem.Status == ProblemStatus.Published
            select new { Problem = problem, PublishedVersion = publishedVersion };

        string? term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            string pattern = $"%{term}%";
            query = query.Where(row =>
                EF.Functions.ILike(
                    row.PublishedVersion != null ? row.PublishedVersion.Title.Value : row.Problem.Title.Value,
                    pattern
                ) || EF.Functions.ILike(row.Problem.Slug.Value, pattern)
            );
        }

        int total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(row => row.Problem.Id)
            .Skip(offset)
            .Take(pagination.Size)
            .Select(row => new ProblemListRowDto(
                row.Problem.Id,
                row.Problem.Slug.Value,
                row.PublishedVersion != null ? row.PublishedVersion.Title.Value : row.Problem.Title.Value,
                row.PublishedVersion != null ? row.PublishedVersion.Difficulty.Value : row.Problem.Difficulty.Value,
                row.Problem.Tags.Select(t => t.Name.Value).ToList(),
                row.Problem.Setups.Select(s => s.LanguageVersionId).Distinct().ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PageResult<ProblemListRowDto>
        {
            Results = items,
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }

    public async Task<PageResult<AdminProblemListRowDto>> GetPoolMembersPagedAsync(
        Guid poolId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default
    )
    {
        var poolItems = context.Set<ProblemPoolItem>().Where(item => EF.Property<Guid>(item, "pool_id") == poolId);

        int total = await poolItems.CountAsync(cancellationToken);

        var items = await poolItems
            .Join(
                context.Problems.IgnoreQueryFilters().AsNoTracking().Where(p => p.DeletedAt == null),
                item => item.ProblemId,
                p => p.Id,
                (item, p) => new { item.AddedAt, Problem = p }
            )
            .OrderByDescending(x => x.AddedAt)
            .Skip((pagination.Page - 1) * pagination.Size)
            .Take(pagination.Size)
            .Select(x => new AdminProblemListRowDto(
                x.Problem.Id,
                x.Problem.Slug.Value,
                x.Problem.Title.Value,
                x.Problem.Difficulty.Value,
                x.Problem.Status,
                x.Problem.TimeLimit.Milliseconds,
                x.Problem.MemoryLimit.Megabytes,
                x.Problem.Tags.Select(t => t.Name.Value).ToList(),
                x.Problem.Setups.Select(s => s.LanguageVersionId).Distinct().ToList(),
                x.Problem.Setups.Count,
                x.Problem.CreatedAt,
                x.Problem.CreatedBy != null ? x.Problem.CreatedBy.Username.Value : null
            ))
            .ToListAsync(cancellationToken);

        return new PageResult<AdminProblemListRowDto>
        {
            Results = items,
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }

    public async Task<Guid?> GetProblemIdByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(setup => setup.Id == problemSetupId)
            .Select(setup => (Guid?)EF.Property<Guid>(setup, "problem_id"))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetRandomProblemIdByDifficultyAsync(
        Guid poolId,
        int minDifficulty,
        int maxDifficulty,
        IReadOnlyCollection<Guid> excludedProblemIds,
        long selectionSeed,
        IReadOnlyCollection<Guid> allowedLanguageVersionIds,
        CancellationToken cancellationToken = default
    )
    {
        excludedProblemIds ??= [];
        var excludedList = excludedProblemIds.ToList();
        allowedLanguageVersionIds ??= [];
        var allowedLanguageVersionIdList = allowedLanguageVersionIds.ToList();

        var poolProblemIds = context
            .Set<ProblemPoolItem>()
            .Where(item => EF.Property<Guid>(item, "pool_id") == poolId)
            .Select(item => item.ProblemId);

        var query = context
            .Problems.AsNoTracking()
            .Where(problem => problem.Status == ProblemStatus.Published)
            .Where(problem => poolProblemIds.Contains(problem.Id))
            .Where(problem => problem.Difficulty.Value >= minDifficulty)
            .Where(problem => problem.Difficulty.Value <= maxDifficulty);

        if (excludedList.Count > 0)
            query = query.Where(problem => !excludedList.Contains(problem.Id));

        if (allowedLanguageVersionIdList.Count > 0)
            query = query.Where(problem =>
                problem.Setups.Any(setup => allowedLanguageVersionIdList.Contains(setup.LanguageVersionId))
            );

        var candidateIds = await query
            .OrderBy(problem => problem.Id)
            .Select(problem => problem.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
            return null;

        int selectedIndex = (int)(unchecked((ulong)selectionSeed) % (ulong)candidateIds.Count);
        return candidateIds[selectedIndex];
    }

    public async Task<IReadOnlyList<ProblemStatusCountDto>> GetStatusBreakdownForCreatorAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Where(problem => problem.DeletedAt == null)
            .AsNoTracking()
            .Where(p => p.CreatedById == creatorId)
            .GroupBy(p => p.Status)
            .Select(group => new ProblemStatusCountDto(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

    public async Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .AnyAsync(problem => problem.Slug == slug && problem.DeletedAt == null, cancellationToken);

    public async Task<ProblemDeletionImpactDto> FindDeletionImpactAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    )
    {
        List<Guid> setupIds = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(setup => EF.Property<Guid>(setup, "problem_id") == problemId)
            .Select(setup => setup.Id)
            .ToListAsync(cancellationToken);

        int submissionCount = setupIds.Count == 0
            ? 0
            : await context
                .Submissions.AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(submission => setupIds.Contains(submission.ProblemSetupId), cancellationToken);

        int gameRoundCount = await context
            .Games.AsNoTracking()
            .IgnoreQueryFilters()
            .SelectMany(game => game.Rounds)
            .CountAsync(round => round.ProblemId == problemId, cancellationToken);

        int unfinishedGameCount = await context
            .Games.AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(
                game =>
                    (game.Status == GameStatus.Pending || game.Status == GameStatus.Running)
                    && game.Rounds.Any(round => round.ProblemId == problemId),
                cancellationToken
            );

        int dailyChallengeCount = await context
            .DailyChallenges.AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(challenge => challenge.ProblemId == problemId, cancellationToken);

        int campaignUnitCount = await context
            .UnitProblems.AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(unitProblem => unitProblem.ProblemId == problemId, cancellationToken);

        int poolCount = await context
            .Set<ProblemPoolItem>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(item => item.ProblemId == problemId, cancellationToken);

        return new ProblemDeletionImpactDto(
            submissionCount,
            gameRoundCount,
            unfinishedGameCount,
            dailyChallengeCount,
            campaignUnitCount,
            poolCount
        );
    }

    public async Task<Problem?> FindByIdForGameParticipantAsync(
        Guid problemId,
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        bool isTheirRound = await context
            .Games.AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(
                game =>
                    game.Id == gameId
                    && game.Participants.Any(participant => participant.UserId == userId)
                    && game.Rounds.Any(round => round.ProblemId == problemId),
                cancellationToken
            );

        if (!isTheirRound)
            return null;

        return await context
            .Problems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(problem => problem.Id == problemId && problem.Status == ProblemStatus.Published)
            .AsSplitQuery()
            .Include(problem => problem.CreatedBy)
            .Include(problem => problem.Tags)
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.Inputs.OrderBy(input => input.Position))
            .Include(problem => problem.Setups)
                .ThenInclude(setup => setup.TestSuites)
                    .ThenInclude(testSuite => testSuite.TestCases)
                        .ThenInclude(testCase => testCase.ExpectedOutputs)
            .Include(problem => problem.Versions)
                .ThenInclude(version => version.Setups)
            .FirstOrDefaultAsync(cancellationToken);
    }
}