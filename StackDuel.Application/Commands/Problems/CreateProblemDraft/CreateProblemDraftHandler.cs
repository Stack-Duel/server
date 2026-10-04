using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Tracks;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.RequiredLanguages;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Commands.Problems.CreateProblemDraft;

internal sealed class CreateProblemDraftHandler(
    IValidator<CreateProblemDraftCommand> validator,
    IProblemRepository problemRepository,
    IExecutionPipelineRepository executionPipelineRepository,
    IRequiredProblemLanguageRepository requiredProblemLanguageRepository,
    ITrackReadRepository trackReadRepository,
    UserContext userContext
) : AbstractCommandHandler<CreateProblemDraftCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateProblemDraftCommand request,
        CancellationToken cancellationToken
    )
    {
        Title title;
        Question question;
        Difficulty difficulty;
        TimeLimit timeLimit;
        MemoryLimit memoryLimit;
        List<string> desiredTagNames;

        try
        {
            title = new Title(request.Title);
            question = new Question(request.Question);
            difficulty = new Difficulty(request.Difficulty);
            timeLimit = new TimeLimit(request.TimeLimitMs);
            memoryLimit = new MemoryLimit(request.MemoryLimitMb);
            desiredTagNames =
            [
                .. request.Tags.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct(),
            ];

            foreach (string name in desiredTagNames)
                _ = new Tag(name);
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
            return Result.Invalid(new ValidationError(ex.Message));
        }

        Guid? pipelineId = await executionPipelineRepository.FindIdByNameAsync(
            WellKnownExecutionPipelines.DefaultName,
            cancellationToken
        );

        if (pipelineId is null)
            return Result.Error("No execution pipeline is configured.");

        Track? track = await trackReadRepository.FindByIdAsync(request.TrackId, cancellationToken);

        if (track is null || !track.IsActive)
            return Result.Invalid(new ValidationError("Track is invalid or inactive."));

        Slug baseSlug = Slug.FromTitle(title);
        Slug slug = baseSlug;
        int suffix = 2;

        while (await problemRepository.FindBySlugAsync(slug, cancellationToken) is not null)
        {
            slug = new Slug($"{baseSlug.Value}-{suffix}");
            suffix++;
        }

        Problem problem = new(slug, title, question, difficulty, timeLimit, memoryLimit);
        problem.SetTrack(track.Id);

        if (userContext.User is not null)
            problem.SetCreatedBy(userContext.User.Id);

        if (desiredTagNames.Count > 0)
        {
            IReadOnlyCollection<ProblemTag> tags = await problemRepository.FindOrCreateTagsAsync(
                desiredTagNames,
                cancellationToken
            );
            foreach (ProblemTag tag in tags)
                problem.AddTag(tag);
        }

        IReadOnlyList<RequiredProblemLanguage> requiredLanguages =
            await requiredProblemLanguageRepository.GetAllOrderedForTrackAsync(track.Id, cancellationToken);

        foreach (RequiredProblemLanguage required in requiredLanguages)
            problem.AddSetup(required.LanguageVersionId, PlaceholderInitialCode, functionName: null, pipelineId.Value);

        await problemRepository.AddAsync(problem, cancellationToken);

        return Result.Success(problem.Id);
    }

    private const string PlaceholderInitialCode = "// TODO: starter code";
}