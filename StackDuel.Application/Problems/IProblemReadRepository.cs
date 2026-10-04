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

    /// <summary>
    /// Picks a problem in the given difficulty range, excluding the given ids. Selection is
    /// deterministic for a given <paramref name="selectionSeed"/> and set of matching problems —
    /// the same seed always yields the same problem — so callers that need multiple participants
    /// in the same game to land on the same problem (e.g. a shared round in Duel/FFA) can derive
    /// the seed from the game and round rather than the problem being picked independently at
    /// random for each participant.
    /// </summary>
    /// <param name="allowedLanguageVersionIds">
    /// Restricts candidates to problems with a setup for at least one of these language versions
    /// (e.g. the tracks/languages a player selected for the game). Empty means unrestricted.
    /// </param>
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

    /// <summary>
    /// Returns the pool's published members' ids, ordered by their position in the pool. Used by the
    /// daily-challenge job for strict sequential assignment (as opposed to <see cref="GetRandomProblemIdByDifficultyAsync"/>,
    /// which game modes use for random selection).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetOrderedEligibleProblemIdsAsync(Guid poolId, CancellationToken cancellationToken = default);
}