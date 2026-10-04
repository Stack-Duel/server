using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Problems.UpdateProblem;

internal sealed class UpdateProblemHandler(
    IValidator<UpdateProblemCommand> validator,
    IProblemRepository problemRepository
) : AbstractCommandHandler<UpdateProblemCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (problem.Status == ProblemStatus.Pending)
            return Result.Invalid(
                new ValidationError("Status", "Cannot edit a problem while validation is in progress.")
            );

        var (parseFailure, content) = ParseContent(request);
        if (parseFailure is not null)
            return parseFailure;

        Result? transitionFailure = ValidateStatusTransition(problem.Status, request.Status);
        if (transitionFailure is not null)
            return transitionFailure;

        problem.UpdateContent(
            content!.Title,
            content.Question,
            content.Difficulty,
            content.TimeLimit,
            content.MemoryLimit
        );

        await SyncTagsAsync(problem, content.TagNames, cancellationToken);

        ApplyStatusTransition(problem, request.Status);

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }

    private static (Result? Failure, ParsedContent? Content) ParseContent(UpdateProblemCommand request)
    {
        try
        {
            List<string> desiredTagNames =
            [
                .. request.Tags.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct(),
            ];

            // Validate tag formatting up front so a bad tag doesn't fail after the content
            // update below has already mutated the tracked entity.
            foreach (string name in desiredTagNames)
                _ = new Tag(name);

            return (
                null,
                new ParsedContent(
                    new Title(request.Title),
                    new Question(request.Question),
                    new Difficulty(request.Difficulty),
                    new TimeLimit(request.TimeLimitMs),
                    new MemoryLimit(request.MemoryLimitMb),
                    desiredTagNames
                )
            );
        }
        catch (Exception ex)
            when (ex
                    is InvalidTitleException
                        or InvalidQuestionException
                        or InvalidDifficultyException
                        or InvalidTimeLimitException
                        or InvalidMemoryLimitException
                        or InvalidTagException
            )
        {
            return (Result.Invalid(new ValidationError(ex.Message)), null);
        }
    }

    private static Result? ValidateStatusTransition(ProblemStatus currentStatus, ProblemStatus? targetStatus)
    {
        if (targetStatus is not { } status || status == currentStatus)
            return null;

        bool isValidTransition =
            (currentStatus == ProblemStatus.Draft && status == ProblemStatus.Published)
            || (currentStatus is ProblemStatus.Draft or ProblemStatus.Published && status == ProblemStatus.Archived);

        return isValidTransition
            ? null
            : Result.Invalid(
                new ValidationError("Status", $"Cannot transition a problem from {currentStatus} to {status}.")
            );
    }

    private async Task SyncTagsAsync(Problem problem, List<string> desiredTagNames, CancellationToken cancellationToken)
    {
        HashSet<string> currentTagNames = [.. problem.Tags.Select(t => t.Name.Value)];

        List<ProblemTag> tagsToRemove = [.. problem.Tags.Where(t => !desiredTagNames.Contains(t.Name.Value))];
        foreach (ProblemTag tag in tagsToRemove)
            problem.RemoveTag(tag);

        List<string> tagNamesToAdd = [.. desiredTagNames.Where(name => !currentTagNames.Contains(name))];
        if (tagNamesToAdd.Count > 0)
        {
            IReadOnlyCollection<ProblemTag> tags = await problemRepository.FindOrCreateTagsAsync(
                tagNamesToAdd,
                cancellationToken
            );
            foreach (ProblemTag tag in tags)
                problem.AddTag(tag);
        }
    }

    private static void ApplyStatusTransition(Problem problem, ProblemStatus? newStatus)
    {
        if (newStatus is not { } status || status == problem.Status)
            return;

        if (status == ProblemStatus.Published)
            problem.Publish();
        else if (status == ProblemStatus.Archived)
            problem.Archive();
    }

    private sealed record ParsedContent(
        Title Title,
        Question Question,
        Difficulty Difficulty,
        TimeLimit TimeLimit,
        MemoryLimit MemoryLimit,
        List<string> TagNames
    );
}