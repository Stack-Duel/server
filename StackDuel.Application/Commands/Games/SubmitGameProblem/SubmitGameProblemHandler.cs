using Ardalis.Result;
using FluentValidation;
using MediatR;
using StackDuel.Application.Commands.Submissions.CreateSubmission;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Commands.Games.SubmitGameProblem;

internal sealed class SubmitGameProblemHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    IMediator mediator,
    IValidator<SubmitGameProblemCommand> validator
) : AbstractCommandHandler<SubmitGameProblemCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        SubmitGameProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        var game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
        if (game is null)
            return Result<Guid>.NotFound($"Game '{request.GameId}' was not found.");

        var participant = game.Participants.FirstOrDefault(p => p.UserId == request.RequestedByUserId);
        if (participant is null)
            return Result<Guid>.Forbidden();

        if (game.CompleteIfExpired(DateTime.UtcNow))
        {
            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
        }

        if (game.Status != GameStatus.Running)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(game.Status), "Only running games can accept submissions.")
            );

        if (participant.HasStoppedPlaying)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(participant.HasStoppedPlaying),
                    participant.HasForfeited
                        ? "You have forfeited this game."
                        : "You have already completed all available problems."
                )
            );

        if (participant.ProblemSession is null)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(participant.ProblemSession), "Problem session not initialized.")
            );

        if (participant.ProblemSession.CurrentProblemId != request.ProblemId)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(request.ProblemId),
                    "The specified problem is not the participant's current problem."
                )
            );

        // Create the grade submission through the existing submission pipeline.
        var submissionResult = await mediator.Send(
            new CreateSubmissionCommand(
                request.ProblemSetupId,
                SubmissionType.Submit,
                request.Code,
                request.RequestedByUserId,
                null,
                GameId: request.GameId
            ),
            cancellationToken
        );

        if (!submissionResult.IsSuccess)
            return Result<Guid>.Error("Failed to create submission.");

        // Stamp the submission ID on the session so completeProblem can verify it came from here.
        participant.SetActiveSubmission(submissionResult.Value);
        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);

        return Result<Guid>.Success(submissionResult.Value);
    }
}