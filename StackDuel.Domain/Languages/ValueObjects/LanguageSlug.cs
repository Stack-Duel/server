using StackDuel.Domain.Languages.Exceptions;
using System.Text.RegularExpressions;

namespace StackDuel.Domain.Languages.ValueObjects;

public sealed record LanguageSlug
{
    private static readonly Regex ValidSlugPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex InvalidSlugCharactersPattern = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex MultipleWhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphensPattern = new(@"-+", RegexOptions.Compiled);

    public LanguageSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidLanguageSlugException("Slug cannot be empty.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidLanguageSlugException($"Slug must be between {MinLength} and {MaxLength} characters.");

        if (!ValidSlugPattern.IsMatch(value))
            throw new InvalidLanguageSlugException(
                "Slug must be lowercase, contain only letters, numbers, and hyphens, and cannot start or end with a hyphen."
            );

        Value = value;
    }

    public static LanguageSlug FromName(LanguageName name)
    {
        string slug = name.Value.ToLowerInvariant();
        slug = InvalidSlugCharactersPattern.Replace(slug, string.Empty);
        slug = MultipleWhitespacePattern.Replace(slug, "-");
        slug = MultipleHyphensPattern.Replace(slug, "-");
        slug = slug.Trim('-');
        return new LanguageSlug(slug);
    }

    public static implicit operator string(LanguageSlug slug) => slug.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 100;
    public static readonly int MinLength = 1;
    public string Value { get; }
}