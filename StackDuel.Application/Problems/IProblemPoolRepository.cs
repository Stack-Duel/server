using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Problems;

public interface IProblemPoolRepository
{
    Task<ProblemPool?> FindByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemPoolDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPoolKeysForProblemAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(ProblemPool pool, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProblemPool pool, CancellationToken cancellationToken = default);
}