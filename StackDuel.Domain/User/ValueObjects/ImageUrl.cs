using StackDuel.Domain.User.Exceptions;

namespace StackDuel.Domain.User.ValueObjects;

public sealed record ImageUrl
{
    public ImageUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidImageUrlException("Image URL cannot be empty.");

        if (value.Length > MaxLength)
            throw new InvalidImageUrlException($"Image URL cannot exceed {MaxLength} characters.");

        if (
            !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        )
            throw new InvalidImageUrlException("Image URL must be a valid HTTP or HTTPS URL.");

        Value = value;
    }

    public static implicit operator string(ImageUrl url) => url.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 2048;

    public string Value { get; }
}