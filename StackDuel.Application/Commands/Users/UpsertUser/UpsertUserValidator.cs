using FluentValidation;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Application.Commands.Users.UpsertUser;

internal sealed class UpsertUserValidator : AbstractValidator<UpsertUserCommand>
{
    private const string UsernameAllowedCharacters = @"^[\p{L}\p{Nd}_-]+$";

    public UpsertUserValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(Username.MinLength)
            .MaximumLength(Username.MaxLength)
            .Matches(UsernameAllowedCharacters)
            .WithMessage("Username can only contain letters, digits, underscores, or hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.Username));

        RuleFor(x => x.ImageUrl)
            .MaximumLength(ImageUrl.MaxLength)
            .Must(BeAnAbsoluteHttpUrl)
            .WithMessage("Image URL must be a valid HTTP or HTTPS URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.Bio).MaximumLength(Bio.MaxLength);
        RuleFor(x => x.LanguageIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Language preferences cannot contain duplicates.");
    }

    private static bool BeAnAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}