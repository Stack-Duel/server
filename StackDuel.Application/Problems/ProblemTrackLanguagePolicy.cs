using StackDuel.Application.Languages;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Problems;

public interface IProblemTrackLanguagePolicy
{
    Task<bool> AllowsLanguageVersionAsync(
        Problem problem,
        Guid languageVersionId,
        CancellationToken cancellationToken
    );
}

internal sealed class ProblemTrackLanguagePolicy(ILanguageReadRepository languageReadRepository)
    : IProblemTrackLanguagePolicy
{
    public async Task<bool> AllowsLanguageVersionAsync(
        Problem problem,
        Guid languageVersionId,
        CancellationToken cancellationToken
    )
    {
        if (!problem.TrackId.HasValue)
            return false;

        IReadOnlyList<Language> trackLanguages = await languageReadRepository.GetActiveLanguagesByTrackIdsAsync(
            [problem.TrackId.Value],
            cancellationToken
        );

        IReadOnlyList<Guid> allowedVersionIds = await languageReadRepository.GetLanguageVersionIdsByLanguageIdsAsync(
            trackLanguages.Select(language => language.Id),
            cancellationToken
        );

        return allowedVersionIds.Contains(languageVersionId);
    }
}