using FluentValidation;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Commands.Problems.CreateProblemDraft;

internal sealed class CreateProblemDraftValidator : AbstractValidator<CreateProblemDraftCommand>
{
    public CreateProblemDraftValidator()
    {
        RuleFor(x => x.Title).NotEmpty().Length(Title.MinLength, Title.MaxLength);
        RuleFor(x => x.Question).NotEmpty().Length(Question.MinLength, Question.MaxLength);
        RuleFor(x => x.Difficulty).GreaterThanOrEqualTo(Difficulty.MinValue);
        RuleFor(x => x.TimeLimitMs).InclusiveBetween(TimeLimit.MinMilliseconds, TimeLimit.MaxMilliseconds);
        RuleFor(x => x.MemoryLimitMb).InclusiveBetween(MemoryLimit.MinMegabytes, MemoryLimit.MaxMegabytes);
        RuleFor(x => x.Tags).NotNull();
        RuleForEach(x => x.Tags).NotEmpty().Length(Tag.MinLength, Tag.MaxLength);
        RuleFor(x => x.TrackId).NotEmpty();
    }
}