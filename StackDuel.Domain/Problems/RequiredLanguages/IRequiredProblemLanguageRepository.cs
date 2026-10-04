using StackDuel.Domain.Problems.RequiredLanguages.Entities;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.RequiredLanguages;

public interface IRequiredProblemLanguageRepository : IRepository<RequiredProblemLanguage>
{
    Task<IReadOnlyList<RequiredProblemLanguage>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequiredProblemLanguage>> GetAllOrderedForTrackAsync(
        Guid trackId,
        CancellationToken cancellationToken = default
    );
    Task<RequiredProblemLanguage?> FindByLanguageVersionIdAsync(
        Guid languageVersionId,
        CancellationToken cancellationToken = default
    );
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}