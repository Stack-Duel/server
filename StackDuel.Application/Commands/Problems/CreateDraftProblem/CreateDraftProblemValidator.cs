using FluentValidation;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Commands.Problems.CreateDraftProblem;

internal sealed class CreateDraftProblemValidator : AbstractValidator<CreateDraftProblemCommand>
{
    public CreateDraftProblemValidator()
    {
        RuleFor(x => x.Input.Title).NotEmpty().Length(Title.MinLength, Title.MaxLength);
        RuleFor(x => x.Input.Question).NotEmpty().Length(Question.MinLength, Question.MaxLength);
        RuleFor(x => x.Input.Difficulty).GreaterThanOrEqualTo(Difficulty.MinValue);
        RuleFor(x => x.Input.TimeLimitMs).InclusiveBetween(TimeLimit.MinMilliseconds, TimeLimit.MaxMilliseconds);
        RuleFor(x => x.Input.MemoryLimitMb).InclusiveBetween(MemoryLimit.MinMegabytes, MemoryLimit.MaxMegabytes);
        RuleFor(x => x.Input.Tags)
            .NotNull()
            .Must(tags => tags.Count() is >= Problem.MinTagCount and <= Problem.MaxTagCount)
            .WithMessage($"A problem must have between {Problem.MinTagCount} and {Problem.MaxTagCount} tags.");
        RuleForEach(x => x.Input.Tags)
            .NotEmpty()
            .Length(Tag.MinLength, Tag.MaxLength)
            .Matches(Tag.ValidTagPattern);
        RuleFor(x => x.Input.TrackId).NotEmpty();
    }
}