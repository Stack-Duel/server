using StackDuel.Domain.Languages.Entities;

namespace StackDuel.Application.Languages;

public interface ILanguageReadRepository
{
    Task<IEnumerable<Language>> FindLanguagesByVersionId(
        IEnumerable<Guid> versionIds,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<Language>> GetActiveLanguagesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Language>> GetActiveLanguagesByTrackIdsAsync(
        IEnumerable<Guid> trackIds,
        CancellationToken cancellationToken
    );
    Task<IReadOnlyList<Guid>> GetLanguageVersionIdsByLanguageIdsAsync(
        IEnumerable<Guid> languageIds,
        CancellationToken cancellationToken
    );
}