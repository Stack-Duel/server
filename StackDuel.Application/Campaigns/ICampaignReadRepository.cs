using StackDuel.Application.Pagination;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Campaigns;

public interface ICampaignReadRepository
{
    Task<Campaign?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<Campaign?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Campaign?> FindByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Campaign>> GetPublishedPathAsync(CancellationToken cancellationToken = default);

    Task<PageResult<Campaign>> GetAdminPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    );
}