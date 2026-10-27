using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Services.Problems;
using StackDuel.Application.Tracks;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Commands.Problems.CreateDraftProblem;

internal sealed class CreateDraftProblemHandler(
    IValidator<CreateDraftProblemCommand> validator,
    ITrackReadRepository trackReadRepository,
    IProblemRepository problemRepository,
    ISlugService slugService,
    IProblemLimitService problemLimitService,
    IUnitOfWork unitOfWork,
    IAdvisoryLockService advisoryLockService,
    UserContext userContext
) : AbstractCommandHandler<CreateDraftProblemCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateDraftProblemCommand command,
        CancellationToken cancellationToken
    )
    {
        var title = new Title(command.Input.Title);
        var question = new Question(command.Input.Question);
        var difficulty = new Difficulty(command.Input.Difficulty);
        var timeLimit = new TimeLimit(command.Input.TimeLimitMs);
        var memoryLimit = new MemoryLimit(command.Input.MemoryLimitMb);
        var desiredTags = command.Input.Tags.Select(t => new Tag(t)).ToList();

        await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await advisoryLockService.AcquireUserScopedLockAsync(userContext.User!.Id, cancellationToken);

        if (await problemLimitService.HasReachedOpenProblemLimitAsync(userContext.User!.Id, cancellationToken))
        {
            return Result.Invalid(
                new ValidationError(
                    $"Reached the {Problem.MaxOpenProblemsPerCreator}-problem limit. Submit or archive one first."
                )
            );
        }

        var track = await trackReadRepository.FindByIdAsync(command.Input.TrackId, cancellationToken);
        if (track?.IsActive is not true)
        {
            return Result.Invalid(
                new ValidationError("This track is not active and cannot be used to create a problem.")
            );
        }

        Slug slug = slugService.GenerateDraftSlug();

        IReadOnlyCollection<ProblemTag> tags = await problemRepository.FindOrCreateTagsAsync(
            desiredTags,
            cancellationToken
        );

        Problem problem = new(slug, title, question, difficulty, timeLimit, memoryLimit);
        problem.SetTrack(track.Id);

        problem.SetCreatedBy(userContext.User!.Id);

        foreach (ProblemTag tag in tags)
            problem.AddTag(tag);

        await problemRepository.AddAsync(problem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(problem.Id);
    }
}