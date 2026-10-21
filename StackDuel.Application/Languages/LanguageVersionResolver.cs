using Ardalis.Result;
using StackDuel.Domain.Languages.Entities;

namespace StackDuel.Application.Languages;

public sealed record LanguageVersionReference(Guid? LanguageVersionId, string? Language, string? Version);

public interface ILanguageVersionResolver
{
    Task<Result<Guid>> ResolveAsync(LanguageVersionReference reference, CancellationToken cancellationToken = default);
}

internal sealed class LanguageVersionResolver(ILanguageReadRepository languageReadRepository)
    : ILanguageVersionResolver
{
    public async Task<Result<Guid>> ResolveAsync(
        LanguageVersionReference reference,
        CancellationToken cancellationToken = default
    )
    {
        if (reference.LanguageVersionId is { } languageVersionId && languageVersionId != Guid.Empty)
            return Result<Guid>.Success(languageVersionId);

        if (string.IsNullOrWhiteSpace(reference.Language))
            return Result<Guid>.Invalid(
                new ValidationError("Provide either a languageVersionId or a language name.")
            );

        IReadOnlyList<Language> languages = [.. await languageReadRepository.GetActiveLanguagesAsync(cancellationToken)];

        string requested = reference.Language.Trim();

        List<Language> matches =
        [
            .. languages.Where(language =>
                language.Slug.Value.Equals(requested, StringComparison.OrdinalIgnoreCase)
                || language.Name.Value.Equals(requested, StringComparison.OrdinalIgnoreCase)
            ),
        ];

        if (matches.Count == 0)
            return Result<Guid>.Invalid(
                new ValidationError($"Unknown language '{requested}'. Available languages: {DescribeLanguages(languages)}.")
            );

        if (matches.Count > 1)
            return Result<Guid>.Invalid(
                new ValidationError($"Language '{requested}' matches more than one language. Use a language slug instead.")
            );

        return ResolveVersion(matches[0], reference.Version);
    }

    private static Result<Guid> ResolveVersion(Language language, string? requestedVersion)
    {
        List<LanguageVersionEntry> activeVersions = [.. language.Versions.Where(version => version.IsActive)];

        if (activeVersions.Count == 0)
            return Result<Guid>.Invalid(
                new ValidationError($"Language '{language.Slug.Value}' has no active versions.")
            );

        if (string.IsNullOrWhiteSpace(requestedVersion))
        {
            return activeVersions.Count == 1
                ? Result<Guid>.Success(activeVersions[0].Id)
                : Result<Guid>.Invalid(
                    new ValidationError(
                        $"Language '{language.Slug.Value}' has several active versions, so a version is required. Available versions: {DescribeVersions(activeVersions)}."
                    )
                );
        }

        string requested = requestedVersion.Trim();

        List<LanguageVersionEntry> matches =
        [
            .. activeVersions.Where(version => version.Version.Value.Equals(requested, StringComparison.OrdinalIgnoreCase)),
        ];

        if (matches.Count == 0)
            return Result<Guid>.Invalid(
                new ValidationError(
                    $"Language '{language.Slug.Value}' has no active version '{requested}'. Available versions: {DescribeVersions(activeVersions)}."
                )
            );

        if (matches.Count > 1)
            return Result<Guid>.Invalid(
                new ValidationError(
                    $"Language '{language.Slug.Value}' has more than one active version labelled '{requested}'. Use a languageVersionId instead."
                )
            );

        return Result<Guid>.Success(matches[0].Id);
    }

    private static string DescribeLanguages(IReadOnlyList<Language> languages) =>
        string.Join(", ", languages.Select(language => language.Slug.Value).Order());

    private static string DescribeVersions(IReadOnlyList<LanguageVersionEntry> versions) =>
        string.Join(", ", versions.Select(version => version.Version.Value).Order());
}