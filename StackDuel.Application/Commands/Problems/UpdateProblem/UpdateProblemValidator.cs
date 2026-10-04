using FluentValidation;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Commands.Problems.UpdateProblem;

internal sealed class UpdateProblemValidator : AbstractValidator<UpdateProblemCommand>
{
    public UpdateProblemValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().Length(Title.MinLength, Title.MaxLength);
        RuleFor(x => x.Question).NotEmpty().Length(Question.MinLength, Question.MaxLength);
        RuleFor(x => x.Difficulty).GreaterThanOrEqualTo(Difficulty.MinValue);
        RuleFor(x => x.TimeLimitMs).InclusiveBetween(TimeLimit.MinMilliseconds, TimeLimit.MaxMilliseconds);
        RuleFor(x => x.MemoryLimitMb).InclusiveBetween(MemoryLimit.MinMegabytes, MemoryLimit.MaxMegabytes);
        RuleFor(x => x.Tags).NotNull();
        RuleForEach(x => x.Tags).NotEmpty().Length(Tag.MinLength, Tag.MaxLength);
    }
}