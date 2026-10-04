using StackDuel.Application.Campaigns;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Campaigns;

internal sealed class CampaignReadRepository(StackDuelDbContext context) : ICampaignReadRepository
{
    public async Task<Campaign?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await FullGraph()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.Status == CampaignStatus.Published, cancellationToken);

    public async Task<Campaign?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await FullGraph().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Campaign?> FindByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default) =>
        await FullGraph()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Modules.Any(m => m.Units.Any(u => u.Id == unitId)), cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        await context.Campaigns.AsNoTracking().AnyAsync(c => c.Slug == slug, cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Campaigns.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Campaign>> GetPublishedPathAsync(CancellationToken cancellationToken = default) =>
        await FullGraph()
            .AsNoTracking()
            .Where(c => c.Status == CampaignStatus.Published)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<PageResult<Campaign>> GetAdminPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<Campaign> query = FullGraph().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{term}%") || EF.Functions.ILike(c.Slug, $"%{term}%")
            );
        }

        int total = await query.CountAsync(cancellationToken);

        List<Campaign> results = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Title)
            .Skip((pagination.Page - 1) * pagination.Size)
            .Take(pagination.Size)
            .ToListAsync(cancellationToken);

        return new PageResult<Campaign>
        {
            Results = results,
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }

    private IQueryable<Campaign> FullGraph() =>
        context
            .Campaigns.Include(c => c.Modules)
                .ThenInclude(m => m.Units)
                    .ThenInclude(u => u.Problems)
            .Include(c => c.Prerequisites)
            .AsSplitQuery();
}