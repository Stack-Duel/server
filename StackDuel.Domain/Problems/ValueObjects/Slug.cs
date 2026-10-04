using StackDuel.Domain.Problems.Exceptions;
using System.Text.RegularExpressions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record Slug
{
    private static readonly Regex ValidSlugPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex InvalidSlugCharactersPattern = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex MultipleWhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphensPattern = new(@"-+", RegexOptions.Compiled);

    public Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidSlugException("Slug cannot be empty.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidSlugException($"Slug must be between {MinLength} and {MaxLength} characters.");

        if (!ValidSlugPattern.IsMatch(value))
            throw new InvalidSlugException(
                "Slug must be lowercase, contain only letters, numbers, and hyphens, and cannot start or end with a hyphen."
            );

        Value = value;
    }

    public static Slug FromTitle(Title title)
    {
        string slug = title.Value.ToLowerInvariant();
        slug = InvalidSlugCharactersPattern.Replace(slug, string.Empty);
        slug = MultipleWhitespacePattern.Replace(slug, "-");
        slug = MultipleHyphensPattern.Replace(slug, "-");
        slug = slug.Trim('-');
        return new Slug(slug);
    }

    public static implicit operator string(Slug slug) => slug.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 200;
    public static readonly int MinLength = 3;
    public string Value { get; }
}