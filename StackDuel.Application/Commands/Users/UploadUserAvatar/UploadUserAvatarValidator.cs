using FluentValidation;
using StackDuel.Application.Images;

namespace StackDuel.Application.Commands.Users.UploadUserAvatar;

internal sealed class UploadUserAvatarValidator : AbstractValidator<UploadUserAvatarCommand>
{
    public static readonly int MaxContentBytes = 5 * 1024 * 1024;

    public UploadUserAvatarValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => content.Length <= MaxContentBytes)
            .WithMessage($"Image must be {MaxContentBytes / (1024 * 1024)} MB or smaller.")
            .Must(content => ImageSignature.Detect(content) is not null)
            .WithMessage(
                $"Image must be one of: {string.Join(", ", ImageSignature.SupportedFormats.Select(f => f.ContentType))}."
            );
    }
}