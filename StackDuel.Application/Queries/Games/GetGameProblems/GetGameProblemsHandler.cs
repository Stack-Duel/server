using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Queries.Games.GetGameProblems;

internal sealed class GetGameProblemsHandler(IGameReadRepository gameReadRepository)
    : IQueryHandler<GetGameProblemsQuery, IReadOnlyList<GameProblemHistoryDto>>
{
    public async Task<Result<IReadOnlyList<GameProblemHistoryDto>>> Handle(
        GetGameProblemsQuery request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result<IReadOnlyList<GameProblemHistoryDto>>.NotFound();

        bool isParticipant = game.Participants.Any(p => p.UserId == request.RequestedByUserId);
        if (!isParticipant)
            return Result<IReadOnlyList<GameProblemHistoryDto>>.Forbidden();

        Dictionary<Guid, Guid> problemIdByRoundId = game.Rounds.ToDictionary(r => r.Id, r => r.ProblemId);

        var problemHistories = game
            .Participants.Select(p =>
            {
                List<GameRoundResult> solvedRounds =
                [
                    .. p.RoundResults.Where(result => result.Outcome == GameRoundOutcome.Solved),
                ];

                List<Guid> solvedProblemIds =
                [
                    .. solvedRounds
                        .Where(result => problemIdByRoundId.ContainsKey(result.RoundId))
                        .Select(result => problemIdByRoundId[result.RoundId]),
                ];

                List<SolvedProblemSubmissionDto> solvedProblemSubmissions =
                [
                    .. solvedRounds
                        .Where(result =>
                            result.SolvingSubmissionId.HasValue && problemIdByRoundId.ContainsKey(result.RoundId)
                        )
                        .Select(result => new SolvedProblemSubmissionDto(
                            problemIdByRoundId[result.RoundId],
                            result.SolvingSubmissionId!.Value
                        )),
                ];

                return new GameProblemHistoryDto(p.UserId, solvedProblemIds, solvedProblemSubmissions);
            })
            .ToList();

        return Result<IReadOnlyList<GameProblemHistoryDto>>.Success(problemHistories);
    }
}