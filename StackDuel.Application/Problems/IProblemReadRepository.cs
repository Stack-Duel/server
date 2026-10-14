using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Problems;

public interface IProblemReadRepository
{
    Task<Problem?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<Problem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Problem?> FindByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForAdminAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<PageResult<ProblemListRowDto>> GetPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    );

    Task<PageResult<AdminProblemListRowDto>> GetAdminProblemsPagedAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<AdminProblemListRowDto>> FindByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default
    );

    Task<PageResult<AdminProblemListRowDto>> GetPoolMembersPagedAsync(
        Guid poolId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Guid>> GetAdminProblemIdsMatchingAsync(
        string? search,
        CancellationToken cancellationToken = default
    );

    Task<Guid?> GetRandomProblemIdByDifficultyAsync(
        Guid poolId,
        int minDifficulty,
        int maxDifficulty,
        IReadOnlyCollection<Guid> excludedProblemIds,
        long selectionSeed,
        IReadOnlyCollection<Guid> allowedLanguageVersionIds,
        CancellationToken cancellationToken = default
    );

    Task<ProblemDifficultyLookupDto?> FindDifficultyByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    );

    Task<Guid?> GetProblemIdByProblemSetupIdAsync(Guid problemSetupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetOrderedEligibleProblemIdsAsync(Guid poolId, CancellationToken cancellationToken = default);
}