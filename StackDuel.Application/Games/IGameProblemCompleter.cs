using Ardalis.Result;

namespace StackDuel.Application.Games;

public sealed record GameProblemCompletionDto(int NewScore, Guid? NextProblemId);

/// <summary>
/// Credits a participant for solving their current problem with an accepted submission and moves
/// them on to the next one. Driven by grading finishing — a player submits once and the server
/// scores and advances them when the submission lands, rather than the client having to confirm
/// the solve in a second request.
/// </summary>
public interface IGameProblemCompleter
{
    Task<Result<GameProblemCompletionDto>> CompleteAsync(
        Guid gameId,
        Guid problemId,
        Guid submissionId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default
    );
}