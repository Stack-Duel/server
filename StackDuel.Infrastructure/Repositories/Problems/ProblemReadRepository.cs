using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemReadRepository(StackDuelDbContext context) : IProblemReadRepository
{
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
            .Where(problem => problem.Slug.Value == slug)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Problems.AsNoTracking().AnyAsync(problem => problem.Id == id, cancellationToken);

    public async Task<bool> ExistsForAdminAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(problem => problem.Id == id, cancellationToken);

    public async Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await context
            .Problems.AsNoTracking()
            .Where(p => p.Slug.Value == slug)
            .Select(p => p.Id)
            .SingleOrDefaultAsync(cancellationToken);

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

    public async Task<PageResult<ProblemListRowDto>> GetPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        var query = context.Problems.AsNoTracking().Where(p => p.Status == ProblemStatus.Published);

        string? term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            string pattern = $"%{term}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Title.Value, pattern) || EF.Functions.ILike(p.Slug.Value, pattern)
            );
        }

        int total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Id)
            .Skip(offset)
            .Take(pagination.Size)
            .Select(p => new ProblemListRowDto(
                p.Id,
                p.Slug.Value,
                p.Title.Value,
                p.Difficulty.Value,
                p.Tags.Select(t => t.Name.Value).ToList(),
                p.Setups.Select(s => s.LanguageVersionId).Distinct().ToList()
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

    public async Task<PageResult<AdminProblemListRowDto>> GetAdminProblemsPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        var query = ApplyAdminSearchFilter(context.Problems.IgnoreQueryFilters().AsNoTracking().AsQueryable(), search);

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

    public async Task<IReadOnlyList<AdminProblemListRowDto>> FindByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default
    )
    {
        if (ids.Count == 0)
            return [];

        return await context
            .Problems.IgnoreQueryFilters()
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
                context.Problems.IgnoreQueryFilters().AsNoTracking(),
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

    public async Task<IReadOnlyList<Guid>> GetAdminProblemIdsMatchingAsync(
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyAdminSearchFilter(context.Problems.IgnoreQueryFilters().AsNoTracking().AsQueryable(), search);

        return await query.Select(p => p.Id).ToListAsync(cancellationToken);
    }

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

        // Ordered so the seeded pick below is reproducible across calls — without a stable
        // order, the same seed could land on a different problem if the database returned
        // candidates in a different sequence.
        var candidateIds = await query
            .OrderBy(problem => problem.Id)
            .Select(problem => problem.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
            return null;

        int selectedIndex = (int)(unchecked((ulong)selectionSeed) % (ulong)candidateIds.Count);
        return candidateIds[selectedIndex];
    }

    public async Task<IReadOnlyList<Guid>> GetOrderedEligibleProblemIdsAsync(
        Guid poolId,
        CancellationToken cancellationToken = default
    )
    {
        var poolItems = context
            .Set<ProblemPoolItem>()
            .Where(item => EF.Property<Guid>(item, "pool_id") == poolId);

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
}